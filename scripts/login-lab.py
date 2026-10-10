#!/usr/bin/env python3
"""Bounded loopback-only protocol lab. Never forwards traffic or logs credentials."""
import argparse
import json
import socket
import time


def number(value, width):
    if not 0 <= value < 253 ** width:
        raise ValueError("Number out of range")
    result = bytearray([254] * width)
    for i in range(width):
        result[i] = value % 253 + 1
        value //= 253
        if not value:
            break
    return bytes(result)


def decode(data):
    value, padding = 0, False
    for i, digit in enumerate(data):
        if digit in (0, 255) or (padding and digit != 254):
            raise ValueError("Invalid encoded number")
        if digit == 254:
            padding = True
        else:
            value += (digit - 1) * 253 ** i
    return value


def transform(body, parameter, direction):
    return bytes((b + direction * (parameter + i + 4)) % 256 for i, b in enumerate(body))


def exact(conn, length):
    result = bytearray()
    while len(result) < length:
        part = conn.recv(length - len(result))
        if not part:
            raise EOFError("Client disconnected")
        result.extend(part)
    return bytes(result)


def frame(conn):
    length = decode(exact(conn, 2))
    if not 2 <= length <= 4096:
        raise ValueError("Unexpected frame length")
    return exact(conn, length)


def send(conn, body):
    conn.sendall(number(len(body), 2) + body)


def event(kind, **fields):
    print(json.dumps({"event": kind, **fields}), flush=True)


def credential(text, parameter):
    output = bytearray()
    for i, char in enumerate(text.encode("ascii"), 1):
        value = (char + parameter * i - (12 if i % 2 == 0 else 0)) & 255
        if i % 3 == 0:
            value += 60
            output.extend((65 + value // 576, 65 + value % 576 // 24))
        else:
            output.append(65 + value // 24)
        output.append(65 + value % 24)
    return bytes(output)


def session(conn, deadline):
    conn.settimeout(min(15, max(.1, deadline - time.monotonic())))
    request = frame(conn)
    if len(request) < 14 or request[:2] != b"\xff\xff":
        raise ValueError("Expected current initialization")
    challenge = decode(request[9:12])
    if challenge >= 1_000_000 or decode(request[8:9]) != 113:
        raise ValueError("Unsupported initialization")
    if len(request) != 13 + decode(request[12:13]):
        raise ValueError("Unexpected installation ID length")
    incoming, outgoing = 6, 7 + challenge % 11
    event("initialization", bytes=len(request), version=[decode(request[i:i+1]) for i in (5,6,7)])
    send(conn, bytes([255,255,2,8,9]) + number(incoming,2) + number(7,2)
         + number(42,2) + number(1,3) + bytes([4]))
    acknowledged = False
    for _ in range(128):
        conn.settimeout(min(60, max(.1, deadline - time.monotonic())))
        if time.monotonic() >= deadline:
            return
        body = transform(frame(conn), outgoing, -1)
        if len(body) < 3:
            raise ValueError("Missing client sequence")
        action, family = body[:2]
        event("packet", action=action, family=family, bytes=len(body), sequence=decode(body[2:3]))
        if not acknowledged:
            expected = bytes([3,1,8]) + number(outgoing,2) + number(incoming,2) + number(42,2)
            if body != expected:
                raise ValueError("Acknowledgement differs from expected format")
            acknowledged = True
            send(conn, transform(bytes([244,3,1]), incoming, 1))
        elif (action, family) == (244,3):
            event("synchronization", matches=body[3:] == b"k")
        elif (action, family) == (35, 2):
            parameter = (challenge % 250 + 2 + 4 + 130) & 255
            expected = credential("testuser", 123) + b"\xff" + credential("TestOnly123", parameter) + b"\xff"
            event("dummy_login_comparison", matches=body[3:] == expected,
                  expected_bytes=len(expected), received_bytes=len(body)-3)
            return
        else:
            # Record structural information only. Never write arbitrary payloads,
            # username/password bytes, installation IDs, or reversible captures.
            event("unhandled", action=action, family=family, payload_bytes=len(body)-3)
            return


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seconds", type=int, default=600)
    args = parser.parse_args()
    if not 1 <= args.seconds <= 600:
        parser.error("Duration must be 1–600 seconds")
    deadline = time.monotonic() + args.seconds
    with socket.socket() as listener:
        listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        listener.bind(("127.0.0.1", 18078))
        listener.listen(1)
        event("listening", address="127.0.0.1:18078", seconds=args.seconds,
              instruction="Use dummy credentials only; no traffic is forwarded.")
        for _ in range(5):
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                break
            listener.settimeout(remaining)
            try:
                conn, _ = listener.accept()
            except TimeoutError:
                break
            with conn:
                try:
                    session(conn, deadline)
                except (OSError, EOFError, ValueError) as error:
                    event("closed", reason=str(error))
    event("stopped")


if __name__ == "__main__":
    main()
