#!/usr/bin/env python3
"""Fida 联机 relay（纯字节桥接）。

两个 NAT 后的玩家无法直连时，本进程作为中转：
  - 主机端 GameRelayHost 连进来，发角色字节 'H'
  - 客机端 GameClient 连进来，发角色字节 'C'
  - relay 把两条 TCP 流双向桥接，之后字节互转
relay 不解析任何游戏协议，纯字节转发（无状态、无业务逻辑）。

用法:
    python3 relay.py [--host 0.0.0.0] [--port 8888]
"""
import argparse
import socket
import threading
import sys


def bridge(a: socket.socket, b: socket.socket) -> None:
    """双向字节桥接 a <-> b，直到任一端断开。"""
    def pump(src: socket.socket, dst: socket.socket) -> None:
        try:
            while True:
                data = src.recv(65536)
                if not data:
                    break
                dst.sendall(data)
        except OSError:
            pass
        finally:
            try:
                dst.shutdown(socket.SHUT_WR)
            except OSError:
                pass

    t1 = threading.Thread(target=pump, args=(a, b), daemon=True)
    t2 = threading.Thread(target=pump, args=(b, a), daemon=True)
    t1.start(); t2.start()
    t1.join(); t2.join()


def main() -> int:
    p = argparse.ArgumentParser(description="Fida 联机 relay")
    p.add_argument("--host", default="0.0.0.0")
    p.add_argument("--port", type=int, default=8888)
    args = p.parse_args()

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((args.host, args.port))
    server.listen(4)
    print(f"[relay] 监听 {args.host}:{args.port}", flush=True)

    # 两个角色槽位 + 锁
    lock = threading.Lock()
    host_conn = [None]
    client_conn = [None]

    def try_bridge() -> None:
        with lock:
            h, c = host_conn[0], client_conn[0]
            if not (h and c):
                return
            host_conn[0] = None
            client_conn[0] = None
        print("[relay] 桥接 host <-> client", flush=True)
        bridge(h, c)

    def handle(conn: socket.socket) -> None:
        conn.settimeout(10)
        try:
            b = conn.recv(1)
        except (socket.timeout, OSError):
            conn.close()
            return
        finally:
            conn.settimeout(None)

        if not b:
            conn.close()
            return

        role = b[0]
        if role == ord('H'):
            with lock:
                if host_conn[0] is not None:
                    print("[relay] 主机已存在，拒绝", flush=True)
                    conn.close()
                    return
                host_conn[0] = conn
            print("[relay] 主机已就位，等待客机...", flush=True)
            try_bridge()
        elif role == ord('C'):
            with lock:
                if client_conn[0] is not None:
                    print("[relay] 客机已存在，拒绝", flush=True)
                    conn.close()
                    return
                client_conn[0] = conn
            print("[relay] 客机已就位，等待主机...", flush=True)
            try_bridge()
        else:
            print(f"[relay] 未知角色 {role!r}，关闭", flush=True)
            conn.close()

    def accept_loop() -> None:
        while True:
            conn, addr = server.accept()
            print(f"[relay] 新连接 {addr}", flush=True)
            threading.Thread(target=handle, args=(conn,), daemon=True).start()

    threading.Thread(target=accept_loop, daemon=True).start()
    try:
        while True:
            threading.Event().wait(1)
    except KeyboardInterrupt:
        print("\n[relay] 退出", flush=True)
        server.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
