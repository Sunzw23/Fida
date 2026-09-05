package main

import (
	"io"
	"net"
	"testing"
	"time"
)

func startRelay(t *testing.T) (string, net.Listener) {
	t.Helper()
	relay := NewRelay()
	ln, err := relay.Listen("127.0.0.1:0")
	if err != nil {
		t.Fatalf("listen: %v", err)
	}
	t.Cleanup(func() { ln.Close() })
	return ln.Addr().String(), ln
}

func dial(t *testing.T, addr string) net.Conn {
	t.Helper()
	conn, err := net.Dial("tcp", addr)
	if err != nil {
		t.Fatalf("dial: %v", err)
	}
	conn.SetDeadline(time.Now().Add(5 * time.Second))
	t.Cleanup(func() { conn.Close() })
	return conn
}

func readExact(t *testing.T, conn net.Conn, n int) []byte {
	t.Helper()
	buf := make([]byte, n)
	if _, err := io.ReadFull(conn, buf); err != nil {
		t.Fatalf("read %d bytes: %v", n, err)
	}
	return buf
}

// TestRelayFullHandshake 验证完整握手流程：
// 主机连接→获得房间码→客机用码加入→双方收到信号→双向字节交换。
func TestRelayFullHandshake(t *testing.T) {
	addr, _ := startRelay(t)

	// 主机连接
	hostConn := dial(t, addr)
	hostConn.Write([]byte{'H'})

	// 读取房间码: 0x01 + 4 chars
	codeBuf := readExact(t, hostConn, 5)
	if codeBuf[0] != 0x01 {
		t.Fatalf("expected 0x01, got 0x%02X", codeBuf[0])
	}
	code := string(codeBuf[1:])
	if len(code) != 4 {
		t.Fatalf("expected 4-char code, got %q (len=%d)", code, len(code))
	}
	t.Logf("room code: %s", code)

	// 客机连接
	clientConn := dial(t, addr)
	clientConn.Write([]byte{'C'})
	clientConn.Write(append([]byte{0x01}, []byte(code)...))

	// 客机读取 ack: 0x03
	ackBuf := readExact(t, clientConn, 1)
	if ackBuf[0] != 0x03 {
		t.Fatalf("expected 0x03, got 0x%02X", ackBuf[0])
	}

	// 主机读取 peer-joined: 0x02
	peerBuf := readExact(t, hostConn, 1)
	if peerBuf[0] != 0x02 {
		t.Fatalf("expected 0x02, got 0x%02X", peerBuf[0])
	}

	// 双向字节交换: host → client
	hostConn.Write([]byte("hello"))
	buf := readExact(t, clientConn, 5)
	if string(buf) != "hello" {
		t.Fatalf("expected 'hello', got %q", buf)
	}

	// client → host
	clientConn.Write([]byte("world"))
	buf = readExact(t, hostConn, 5)
	if string(buf) != "world" {
		t.Fatalf("expected 'world', got %q", buf)
	}
}

// TestRelayWrongCode 验证错误房间码被拒绝（连接关闭，无 ack）。
func TestRelayWrongCode(t *testing.T) {
	addr, _ := startRelay(t)

	// 主机连接，获得房间码
	hostConn := dial(t, addr)
	hostConn.Write([]byte{'H'})
	codeBuf := readExact(t, hostConn, 5)
	code := string(codeBuf[1:])

	// 客机用错误码连接
	clientConn := dial(t, addr)
	clientConn.Write([]byte{'C'})

	wrongCode := "XXXX"
	if wrongCode == code {
		wrongCode = "YYYY"
	}
	clientConn.Write(append([]byte{0x01}, []byte(wrongCode)...))

	// 应收到连接关闭（无 0x03 ack）
	buf := make([]byte, 1)
	n, err := clientConn.Read(buf)
	if n > 0 {
		t.Fatalf("expected connection close, got %d bytes: %q", n, buf[:n])
	}
	if err == nil {
		t.Fatal("expected error, got nil")
	}
	t.Logf("correctly rejected wrong code, error: %v", err)
}

// TestRelayMultipleRooms 验证两个独立房间互不干扰。
func TestRelayMultipleRooms(t *testing.T) {
	addr, _ := startRelay(t)

	// 房间 A
	hostA := dial(t, addr)
	hostA.Write([]byte{'H'})
	codeBufA := readExact(t, hostA, 5)
	codeA := string(codeBufA[1:])

	clientA := dial(t, addr)
	clientA.Write([]byte{'C'})
	clientA.Write(append([]byte{0x01}, []byte(codeA)...))
	readExact(t, clientA, 1) // 0x03
	readExact(t, hostA, 1)   // 0x02

	hostA.Write([]byte("A-msg"))
	buf := readExact(t, clientA, 5)
	if string(buf) != "A-msg" {
		t.Fatalf("room A: expected 'A-msg', got %q", buf)
	}

	// 房间 B（独立）
	hostB := dial(t, addr)
	hostB.Write([]byte{'H'})
	codeBufB := readExact(t, hostB, 5)
	codeB := string(codeBufB[1:])
	if codeA == codeB {
		t.Fatal("two rooms got the same code")
	}

	clientB := dial(t, addr)
	clientB.Write([]byte{'C'})
	clientB.Write(append([]byte{0x01}, []byte(codeB)...))
	readExact(t, clientB, 1) // 0x03
	readExact(t, hostB, 1)   // 0x02

	hostB.Write([]byte("B-msg"))
	buf = readExact(t, clientB, 5)
	if string(buf) != "B-msg" {
		t.Fatalf("room B: expected 'B-msg', got %q", buf)
	}
}
