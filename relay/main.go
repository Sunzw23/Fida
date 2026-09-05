// Fida Relay — Go 实现的公网房间码中继服务器。
//
// 协议（角色字节 + 房间码握手）：
//
//   Host   → Relay : 'H'                     (0x48)
//   Relay  → Host  : 0x01 <code[4]>          (分配房间码)
//   Client → Relay : 'C' 0x01 <code[4]>     (加入请求)
//   Relay  → Client: 0x03                    (加入成功)
//   Relay  → Host  : 0x02                    (对手已加入，开始桥接)
//
// 握手完成后，relay 是透明字节管道（双向 io.Copy），不解析游戏帧协议。
// 游戏帧格式 [Type:1B][Len:2B][Payload:NB] 由客户端自行处理。
//
// 编译：go build -ldflags="-s -w" -o relay.exe .
// 运行：./relay.exe [-port 8888]

package main

import (
	"flag"
	"fmt"
	"io"
	"log"
	"math/rand/v2"
	"net"
	"sync"
	"time"
)

// ─── 协议常量 ─────────────────────────────────────────────

const (
	roleHost      = 'H' // 0x48
	roleClient    = 'C' // 0x43
	msgCodeAssign = byte(0x01) // relay→host: 0x01 + 4-char code
	msgPeerJoined = byte(0x02) // relay→host: peer joined, bridge starting
	msgJoinOK     = byte(0x03) // relay→client: join accepted
)

const (
	codeLength   = 4
	handshakeTO  = 10 * time.Second
	hostWaitTO   = 5 * time.Minute
)

// 无歧义字母表（不含 0/O/1/I/L）
const alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ"

// ─── 房间 ─────────────────────────────────────────────────

type Room struct {
	Code          string
	Host          net.Conn
	Client        net.Conn
	ClientJoined  chan struct{} // closed when client registers
	BridgeDone    chan struct{} // closed when bridge finishes
	BridgeStarted bool
}

// ─── Relay ────────────────────────────────────────────────

type Relay struct {
	mu    sync.Mutex
	rooms map[string]*Room
}

func NewRelay() *Relay {
	return &Relay{rooms: make(map[string]*Room)}
}

func (r *Relay) genCode() string {
	b := make([]byte, codeLength)
	for i := range b {
		b[i] = alphabet[rand.IntN(len(alphabet))]
	}
	return string(b)
}

func (r *Relay) assignCode() (string, bool) {
	r.mu.Lock()
	defer r.mu.Unlock()
	for i := 0; i < 100; i++ {
		c := r.genCode()
		if _, exists := r.rooms[c]; !exists {
			return c, true
		}
	}
	return "", false
}

func (r *Relay) removeRoom(code string) {
	r.mu.Lock()
	delete(r.rooms, code)
	r.mu.Unlock()
}

// Listen starts the accept loop on the given address (e.g. ":8888" or "127.0.0.1:0").
func (r *Relay) Listen(addr string) (net.Listener, error) {
	ln, err := net.Listen("tcp", addr)
	if err != nil {
		return nil, err
	}
	go func() {
		for {
			conn, err := ln.Accept()
			if err != nil {
				return
			}
			go r.Handle(conn)
		}
	}()
	return ln, nil
}

// Handle processes a single client connection.
func (r *Relay) Handle(conn net.Conn) {
	defer conn.Close()
	defer func() {
		if rec := recover(); rec != nil {
			log.Printf("panic handling %s: %v", conn.RemoteAddr(), rec)
		}
	}()

	conn.SetReadDeadline(time.Now().Add(handshakeTO))

	role := make([]byte, 1)
	if _, err := io.ReadFull(conn, role); err != nil {
		return
	}

	switch role[0] {
	case roleHost:
		r.handleHost(conn)
	case roleClient:
		r.handleClient(conn)
	default:
		log.Printf("unknown role byte 0x%02X from %s", role[0], conn.RemoteAddr())
	}
}

func (r *Relay) handleHost(conn net.Conn) {
	code, ok := r.assignCode()
	if !ok {
		log.Println("failed to assign room code (all codes in use)")
		return
	}

	room := &Room{
		Code:         code,
		Host:         conn,
		ClientJoined: make(chan struct{}),
		BridgeDone:   make(chan struct{}),
	}

	r.mu.Lock()
	r.rooms[code] = room
	r.mu.Unlock()
	defer r.removeRoom(code)

	// 发送房间码给主机
	if _, err := conn.Write(append([]byte{msgCodeAssign}, code...)); err != nil {
		log.Printf("room %s: send code to host failed: %v", code, err)
		return
	}
	log.Printf("room %s: host %s registered, waiting for client", code, conn.RemoteAddr())

	// 等待客户端加入（或超时）
	select {
	case <-room.ClientJoined:
		log.Printf("room %s: client joined, starting bridge", code)
	case <-time.After(hostWaitTO):
		log.Printf("room %s: host wait timeout, cleaning up", code)
		return
	}

	// 等待桥接结束（无超时——游戏时长）
	<-room.BridgeDone
}

func (r *Relay) handleClient(conn net.Conn) {
	// 读取: 0x01 + 4-char code
	buf := make([]byte, 1+codeLength)
	if _, err := io.ReadFull(conn, buf); err != nil {
		log.Printf("read room code from %s: %v", conn.RemoteAddr(), err)
		return
	}
	if buf[0] != msgCodeAssign {
		log.Printf("expected 0x01, got 0x%02X from %s", buf[0], conn.RemoteAddr())
		return
	}
	code := string(buf[1:])

	r.mu.Lock()
	room, exists := r.rooms[code]
	if exists {
		room.Client = conn
	}
	r.mu.Unlock()

	if !exists {
		log.Printf("room %s: not found (client %s)", code, conn.RemoteAddr())
		return
	}

	log.Printf("room %s: client %s joined", code, conn.RemoteAddr())
	close(room.ClientJoined)

	// 确保桥接只启动一次
	r.mu.Lock()
	if room.BridgeStarted {
		r.mu.Unlock()
		<-room.BridgeDone
		return
	}
	room.BridgeStarted = true
	r.mu.Unlock()

	// 发送 ack 给客机
	if _, err := conn.Write([]byte{msgJoinOK}); err != nil {
		log.Printf("room %s: send join-OK to client failed: %v", code, err)
		close(room.BridgeDone)
		return
	}

	// 通知主机对手已加入
	if _, err := room.Host.Write([]byte{msgPeerJoined}); err != nil {
		log.Printf("room %s: send peer-joined to host failed: %v", code, err)
		close(room.BridgeDone)
		return
	}

	// 启动双向字节桥接
	go func() {
		defer close(room.BridgeDone)
		r.bridge(room.Host, conn)
	}()

	<-room.BridgeDone
}

func (r *Relay) bridge(host, client net.Conn) {
	host.SetReadDeadline(time.Time{})
	client.SetReadDeadline(time.Time{})

	h2cErr := make(chan error, 1)
	c2hErr := make(chan error, 1)

	go func() {
		_, err := io.Copy(client, host)
		if tc, ok := client.(interface{ CloseWrite() error }); ok {
			tc.CloseWrite()
		}
		h2cErr <- err
	}()

	go func() {
		_, err := io.Copy(host, client)
		if tc, ok := host.(interface{ CloseWrite() error }); ok {
			tc.CloseWrite()
		}
		c2hErr <- err
	}()

	h2c := <-h2cErr
	c2h := <-c2hErr
	log.Printf("bridge complete (h2c=%v, c2h=%v)", h2c, c2h)
}

// ─── Main ─────────────────────────────────────────────────

func main() {
	port := flag.Int("port", 8888, "listen port")
	flag.Parse()

	relay := NewRelay()
	ln, err := relay.Listen(fmt.Sprintf(":%d", *port))
	if err != nil {
		log.Fatalf("listen :%d: %v", *port, err)
	}
	defer ln.Close()
	log.Printf("Fida relay listening on :%d (dual-stack)", *port)

	select {}
}
