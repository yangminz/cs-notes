6th by James F. Kurose and Keith W. Ross

# Chapter 2 Application Layer

## 2.1 Principles of Network Applications

### 2.1.1 Network Application Architectures

Client-server architecture: single server --> data center (scalability)

Peer-to-peer (p2p) architecture

### 2.1.2 Processes Communicating

Processes on the same end system (localhost): end sys's OS communication: shared memory, pipe, etc.

Client & server processes

The interface between process & transport-layer protocol: **SOCKET**. The transportation infrastructure. Application controls:

1.  Which transport protocol
2.  Transport layer parameter

Addressing: IP. Endpoint (identify the physical server from network of computers), port number (identify the networking process from server's processes)

## 2.1.3 Transport Services Available to Applications

4 dimensions:

1.  Reliability: when packets are lost
2.  Throughput: bandwidth
3.  Latency: end-to-end delay
4.  Security

### 2.1.4 Transport Services Provided by the Internet

TCP & UDP

### 2.1.5 Application-Layer Protocols

Define:

HyperText Transfer Protocol (HTTP RFC2616)

## 2.2 The Web and HTTP

### 2.2.1 Overview of HTTP

HyperText Transfer Protocol (HTTP) defined in RFC 2616.

-   **connection**: A transport layer virtual circuit established between two programs for the purpose of communication.
-   **message**: The basic unit of HTTP communication, consisting of a structured sequence of octets matching the syntax defined in section 4 and transmitted via the connection.
-   **request**: An HTTP request message, as defined in section 5.
-   **response**: An HTTP response message, as defined in section 6.
-   **resource**: A network data object or service that can be identified by a URI, as defined in section 3.2. Resources may be available in multiple representations (e.g. multiple languages, data formats, size, and resolutions) or vary in other ways.
-   **client**: A program that establishes connections for the purpose of sending requests.
-   **user agent**: The client which initiates a request. These are often browsers, editors, spiders (web-traversing robots), or other end user tools.
-   **server**: An application program that accepts connections in order to service requests by sending back responses. Any given program may be capable of being both a client and a server; our use of these terms refers only to the role being performed by the program for a particular connection, rather than to the program's capabilities in general. Likewise, any server may act as an origin server, proxy, gateway, or tunnel, switching behavior based on the nature of each request. 
-   **origin server**: The server on which a given resource resides or is to be created.
-   **gateway**: A server which acts as an intermediary for some other server. Unlike a proxy, a gateway receives requests as if it were the origin server for the requested resource; the requesting client may not be aware that it is communicating with a gateway.
-   **tunnel**: An intermediary program which is acting as a blind relay between two connections. Once active, a tunnel is not considered a party to the HTTP communication, though the tunnel may have been initiated by an HTTP request. The tunnel ceases to exist when both ends of the relayed connections are closed.
-   **upstream/downstream**: Upstream and downstream describe the flow of a message: all messages flow from upstream to downstream.
-   **inbound/outbound**: Inbound and outbound refer to the request and response paths for messages: "inbound" means "traveling toward the origin server", and "outbound" means "traveling toward the user agent" 

HTTP use TCP as underlying transport protocol.

**Stateless protocol**: server sends responses without storing any state info about the client. Server may do cache, but not buffer in protocol level.

### 2.2.2 Non-Persistent and Persistent Connections

#### Non-persistent connection

Each req/resp a separate TCP connection. E.g. index.html a connection, jpg a connection (it's a independent request (since different URI)), css files a connection, javascript files a connection.

Most modern browsers open 5-10 parallel TCP connections and each handles one req/resp transaction. 

Shortcomings:

1.  Need to establish a new connectin for each request; Client and server needs to store the variables and allocate buffer for these connections -- big burden. 
2.  RTT (Round Trip Time) is more.

#### Persistent connection

All req/resp over the same TCP connection. TCP connection is not closed after server's sending resp. No TCP handshake overhead.

Default mode of HTTP uses persistent connections with pipelining. 

### 2.2.3 HTTP Message Format

This is actually a state machine:

```
HTTP-message    = Request | Response     ; HTTP/1.1 messages

generic-message = start-line
                    *(message-header CRLF)
                    CRLF
                    [ message-body ]

start-line      = Request-Line | Status-Line

Request-Line    = Method SP Request-URI SP HTTP-Version CRLF

Method          = "OPTIONS"
                 | "GET"
                 | "HEAD"
                 | "POST"
                 | "PUT"
                 | "DELETE"
                 | "TRACE"
                 | "CONNECT"

Response        = Status-Line 
                  *(( general-header 
                  | response-header 
                  | entity-header ) CRLF)
                  CRLF
                  [ message-body ]

message-header  = field-name ":" [ field-value ]
field-name      = token
field-value     = *( field-content | LWS )
field-content   = <the OCTETs making up the field-value
                 and consisting of either *TEXT or combinations
                 of token, separators, and quoted-string>
```

Well known resp status code:

```
200 OK
400 Bad Request
404 Not Found
500 Internal Server Error
```

### 2.2.4 User-Server Interaction: Cookies

RFC 6265

4 components:

1.  a cookie header line in the HTTP response message; 
2.  a cookie header line in the HTTP request message;
3.  a cookie file kept on the user’s end system and managed by the user’s browser;
4.  a back-end database at the Web site.

It's used to keep track of users, it's with user state. E.g. it can be used to restrict user's access. --- A user session layer above the stateless HTTP.

### 2.2.5 Web Caching

Web cache/proxy server: 

An intermediary program which acts as both a server and a client for the purpose of making requests on behalf of other clients. Requests are serviced internally or by passing them on, with possible translation, to other servers. A proxy MUST implement both the client and server requirements of this specification. A "transparent proxy" is a proxy that does not modify the request or response beyond what is required for proxy authentication and identification. A "non-transparent proxy" is a proxy that modifies the request or response in order to provide some added service to the user agent, such as group annotation services, media type transformation, protocol reduction, or anonymity filtering. Except where either transparent or non-transparent behavior is explicitly stated, the HTTP proxy requirements apply to both types of proxies. 

```
client =====> proxy =====> origin server =====> proxy =====> client
```

Typically a web cache is purchased and installed by an ISP. Used for 2 reasons:

1.  Reduce response time for a client request. Particularly when there is a bottleneck bandwidth between client and origin server.
2.  Reduce the traffic of the client's institution.

Example, LAN is 100 Mbps, while public internet access link is 15 Mbps.

**Content Distribution Networks (CDNs)**, web cache is important. 

### 2.2.6 The Conditional GET

Used to verify if a web cache is stale. By comparing the last-modified date time.


## 2.4 Electronic Mail in the Internet




## 2.5 DNS - The Internet's Directory Service

RFC 1034 & 1035

Hostname: text, e.g. `www.google.com`. But not knowing the hosting machine/server. So need IP address to identify the hosts.

### 2.5.1 Services Provided by DNS

**Domain Name System** (DNS) to mapping text hostname to IP address. It's:

1.  distributed database implemented in a **hierarchy** (IP address is hierarchical) of DNS servers
2.  application-layer protocol defines the query to the distribtued DB

Usually are UNIX machines running Berkeley Internet Name Domain (BIND). The protocol runs over UDP and uses port 53.

DNS is used by other app-level protocols (HTTP, SMTP, FTP) to map the hostname to IP address. 

DNS will add delay for querying, but usually the domain names are cached in local DNS server, so it's quick.

DNS enables:

-   Hostname aliasing: one canonical host name, and multiple alias.
-   Mail server aliasing: web server and mailbox server can be the same
-   Load balancing: distribute requests to same hostname to different IP addresses

### 2.5.2 Overview of How DNS Works

Client app invokes client-side of DNS, e.g. `gethostbyname()`. But actually DNS service is complex: A large number of DNS servers distrbuted around the world + protocol to define query.

DNS must be _distrbuted_ to scale. It's a great example of implementing _distrbuted DB_.

DNS servers are organized hierarchically: Root DNS servers, Top-level Domain (TLD) DNS servers, e.g. `.com` DNS servers, `.org` DNS servers, `.edu` DNS servers, etc. 

Query to level DNS server will return the IP address to next level DNS server. E.g. Root DNS server will return `.com` DNS server's IP. Finally get the local DNS server and response the IP to host.

13 root DNS servers world-wide. Then TLD DNS servers, e.g. `.com, .edu, .jp`. Then authoritative DNS servers for each org, e.g. `google.com`. 

Local DNS is not strictly belong to the hierarchy. Each ISP provides a local DNS. And the hierarchical query from root server is made by local DNS server.

When DNS server miss the hostname, query the next level in hierarchy.

To reduce query times, use **DNS caching**. But the mapping is not permanent, so the cache entry will expire (Time to live, TTL, usually 2d). Much faster if the local DNS can cache.

### 2.5.3 DNS Records and Messages

**Resource Records (RR)**: Each rr is a 4-tuple:

```
Name        Value               Type        TTL
-----------------------------------------------
hostname    IP address          A
-----------------------------------------------
domain      auth-DNS server's   NS
            hostname for query
            chain
-----------------------------------------------
hostname    canonical hostname  CNAME
            for alias hostname
-----------------------------------------------
hostname    canonical hostname  MX
            of mail server
```

#### DNS Message

DNS req/resp are in the same format:

Header, Name (query, question), RR (Answer), Authoritative servers, additional info

Use `nslookup` to get response from DNS server directly.

### Vulnerabilities

DDoS Bandwidth-flooding attack. Targeting at DNS server, targeting at origin server. 


# Chapter 3 Transport Layer

Central piece of layered arch.

## 3.1 Introduction and Transport-Layer Services

Provide: **logical communication** between the processes running on different hosts, as if the hosts is directly connected. 

Implemented in end systems but not network routers. Split app-layer message into packets (Transport-layer segments) by breaking message into smaller chunks and adding transport-layer header.

```
application level messages
app chunk 1, chunk 2, ...
trans header + app chunk 1, trans header + app chunk 2, ...
network packet{trans header + app chunk 1}, network packet{trans header + app chunk 2}, ...
```

Network routers act only on the network-level packet (datagram).

### 3.1.1 Relationship Between Transport and Network Layers

Trans-level protocol are constrained by the underlying net-level protocols. E.g., delay, bandwidth, trop, etc. TCP can offer reliable data transfer service even when datagram loses. Also can provide encryption, while net-level cannot.

Trans-level protocol themselves are processes running in end-system.

### 3.1.2 Overview of the Transport Layer in the Internet

-   **UDP**: User datagram protocol, unreliable, connectless. Transport-layer multiplexing & error check.
-   **TCP**: Transmission control protocol, reliable, connect-oriented. Transport-layer multiplexing & error check. _Reliable data transfer. Congestion control._

Network-level: **IP, internet protocol**. It's a _best-effort delivery service_. No guarantees that the datagram will be delivered. So it's unreliable.

TCP/UDP extends IP's delivery service, from host-to-host delivery to process-to-process. **Transport-layer multiplexing** and **transport-layver demultiplexing** (by port).

## 3.2 Multiplexing and Demultiplexing

Transport layer receives segments from network layer, then deliver these app-message to corresponding process. Transport layer delivers message to the socket address of network app. 

Demultiplexing: Each trans-layer segment has fields to identify the receiving socket, and will deliver the app message to it.

Multiplexing: Different sockets providing different app message, each encapsulated with header information (segment), passing the segments to network layer.

The multiplexing requires:

1.  Sockets have unique identifier
2.  Each transport layer segment has fields to identify the socket: source port number field & destination port number field.

0-1023 are reserved well known port numbers. 1024-65535 are free to use. 80 for HTTP.

### Connectionless Multiplexing and Demultiplexing

UDP is using 2-tuple socket: (destination IP address, destination port number). So 2 segments with different source IP address and/or port number, but same destination IP address and port number, then will go to the same destination process.

Source port is used return address when destination wants to send back to source.

Example

```
Host A:
    Web Client
        (dest port: 8080,  dest IP: B)    source port: 26145,    source IP: A
Host C:
    Web Client
        (dest port: 8080,  dest IP: B)    source port: 7532,     source IP: C
        (dest port: 8080,  dest IP: B)    source port: 26145,    source IP: C
Server B:
    Web Server: 3 segments go to the same socket
```

### Connection-Oriented Multiplexing and Demultiplexing

Different UDP, TCP socket is 4-tuple: (source IP address, source port number, destination IP address, destination port number). So 2 TCP segments with different source IP addr or source port numbers will be directed to 2 different sockets. 

Example

```
Host A:
    Web Client
        (source port: 26145,    source IP: A,   dest port: 80,  dest IP: B)
Host C:
    Web Client
        (source port: 7532,     source IP: C,   dest port: 80,  dest IP: B)
        (source port: 26145,    source IP: C,   dest port: 80,  dest IP: B)
Server B:
    Web Server: 3 different TCP connections (sockets)
```

### Web Server and TCP

Modern high-performing web servers often use only one process, create a new thread with a new connection socket for each new client. So many connection sockets (with different identifiers) attached to the same process. 

## 3.3 Connectionless Transport: UDP

RFC 768

UDP: minimal transport layer protocol: multiplexing/demultiplexing and light error checking. Adds nothing to IP, app almost directly talks with IP. DNS is using UDP. 

Connectionless: no handshaking.

UDP is good for:

1.  Finer application-level control over what data is sent and when. UDP will immediately send the segment to network layer. While TCP has congestion-control mechanism.
2.  No connection establishment. E.g. DNS uses UDP to avoid 3-way handshaking to reduce latency. HTTP uses TCP to ensure reliability.
3.  No connection state. TCP maintains connection states. So UDP clients is more than TCP.

Application can build relability over UDP. **QUIC**.

### 3.3.1 UDP Segment Structure

RFC 768

The hex numbers:

```
ssss    - 16 bits, source port number
dddd    - 16 bits, destination port number
llll    - 16 bits, length of UDP segment (header + payload)
cccc    - 16 bits, check sum for error detection
pppp    - payload data
pppp
....
```

### 3.3.2 UDP Checksum

Checksum provides for error detection. If the bits within the UDP segment have been altered, e.g., by the noise in links or router.

Calculation:

```
0110011001100000    - a
0101010101010101    - b
---------------------------
1011101110110101    - a+b
1000111100001100    - c
---------------------------
0100101011000001    - a+b+c (overflow)
---------------------------
1011010100111110    - ~(a+b+c) (complement), checksum
```

At receiver, all 16-bit word are added, including the checksum, so it should be `1111111111111111` (can overflow):

```
0110011001100000    - a
0101010101010101    - b
1000111100001100    - c
1011010100111110    - checksum
-------------------------------
1111111111111111
```

Note that checksum can collide.

Because lower layers may not provide error checking, so transport layer needs it.

## 3.4 Principles of Reliable Data Transfer

Reliable Data Transfer (RDT) is not only at transport layer. Reliability is fundamental problem in networking: the transferred data for upper layer is not corrupted (lower layer maybe unreliable, IP). 

### 3.4.1 Building a Reliable Data Transfer Protocol

When packets will be delivered in the order which they were sent ...

#### Reliable Data Transfer over a Perfectly Reliable Channel: rdt1.0

First consider the IP channel is completely reliable.

Finite State Machine (FSM) defines rdt1.0 sender and receiver:

```csharp
public class Sender
{
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callbacks
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        this.NetworkClient.Send(
            new TransportPacket(
                data: e.AppData
            ));
    }
}

public class Receiver
{
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callbacks
    public void OnPacketReceived(object obj, PacketReceivedEventArgs e)
    {
        this.AppClient.Deliver(e.TransportPacket.AppData);
    }
}
``` 

Because IP layer is reliable, so just a encapsulate & decapsulate.

#### Reliable Data Transfer over a Channel with Bit Errors: rdt2.0

Retransmission if failed to ack: **Automatic Repeat ReQuest (ARQ) protocol**. Require 3 capabilities:

1.  Error detection. E.g. error detection field. Further, error correction (limited)
2.  Receiver feedback. Positive (ACK) & negative (NAK). 
3.  Retransmission. If a packet is received in error, sender need to retransmit.

```csharp
public class Sender
{
    public SenderState CurrentState = SenderState.WaitingAppData;
    public TransportPacket CachedForRetransmission;

    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        if (this.CurrentState == WaitingAppData)
        {
            // app calls sending
            // waiting for receiver's ACK
            this.CachedForRetransmission = new TransportPacket(
                data: e.AppData
            );
            this.NetworkClient.Send(this.CachedForRetransmission);

            // state transfer
            this.CurrentState = SenderState.WaitingAck;
        }
        else
        {
            this.AppClient.Reject(e.AppData);
        }
    }

    // callback
    public void OnAckReceived(object obj, AckReceivedEventArgs e)
    {
        if (this.CurrentState == SenderState.WaitingAck)
        {
            switch (e.ACK)
            {
                case ACK:
                    this.CurrentState = WaitingAppData;
                    break;
                case NAK:
                    // retransmit
                    this.NetworkClient.Send(this.CachedForRetransmission);
                    break;
                default:
                    break;
            }
        }
    }
}

public class Receiver
{
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnPacketReceived(object obj, PacketReceivedEventArgs e)
    {
        TransportPacket pkt = e.TransportPacket;

        if (pkt.IsCorrupted() == true)
        {
            // corrupted
            this.NetworkClient.SendNak();
        }
        else
        {
            this.AppClient.Deliver(pkt.AppData);
            this.NetworkClient.SendAck();
        }
    }
}
```

When sender is waiting ACK or NAK (state 2), it cannot get more data from application layer. So it's a _Stop-and-Wait Protocol_.

Problem for rdt2.0: what if ACK/NAK packet is corrupted? Add sequence number. Receiver check the sequence number to determine if the packet is a retransmission. For rdt2.0, 1 bit is enough. 

```csharp
public class Sender
{
    public State CurrentState = WaitingAppDataSeq0;
    public TransportSegment CachedForRetransmission;

    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        if (this.CurrentState == WaitingAppDataSeq0)
        {
            // app calls sending
            // waiting for receiver's ACK
            this.CachedForRetransmission = new TransportPacket(
                data: e.AppData,
                seqNum: 0
            );
            this.NetworkClient.Send(this.CachedForRetransmission);
            this.CurrentState = SenderState.WaitingAckSeq0;
        }
        else if (this.CurrentState == WaitingAppDataSeq1)
        {
            this.CachedForRetransmission = new TransportPacket(
                data: e.AppData,
                seqNum: 1
            );
            this.NetworkClient.Send(this.CachedForRetransmission);
            this.CurrentState = SenderState.WaitingAckSeq1;
        }
    }
    
    // callback
    public void OnAckReceived(object obj, AckReceivedEventArgs e)
    {
        if (this.CurrentState == SenderState.WaitingAck0)
        {
            switch (e.ACK)
            {
                case ACK:
                    this.CurrentState = WaitingAppData1;
                    break;
                case NAK:
                    // retransmit
                    this.NetworkClient.Send(this.CachedForRetransmission);
                    break;
                default:
                    break;
            }
        }
        else if (this.CurrentState == SenderState.WaitingAck1)
        {
            switch (e.ACK)
            {
                case ACK:
                    this.CurrentState = WaitingAppData0;
                    break;
                case NAK:
                    this.NetworkClient.Send(this.CachedForRetransmission);
                    break;
                default:
                    break;
            }
        }
    }
}

public class Receiver
{
    public ReceiverState CurrentState = ReceiverState.WaitingSenderPacketSeq0;

    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnPacketReceived(object obj, PacketReceivedEventArgs e)
    {
        TransportPacket pkt = e.TransportPacket;

        if (pkt.IsCorrupted() == true)
        {
            // corrupted
            this.NetworkClient.SendNak();
        }
        else
        {
            int pktSeqNum = pkt.SeqNum;

            if (this.CurrentState == ReceiverState.WaitingSenderPacketSeq0)
            {
                switch (pktSeqNum)
                {
                    case 0:
                        // waiting for 0 and get 0
                        this.AppClient.Deliver(pkt.AppData);
                        this.NetworkClient.SendAck();
                        this.CurrentState = ReceiverState.WaitingSenderPacketSeq1;
                        break;
                    case 1:
                        // waiting for 0 but get 1
                        this.NetworkClient.SendNak();
                        break;
                    default:
                        break;
                }
            }
            else if (this.CurrentState == ReceiverState.WaitingSenderPacketSeq1)
            {
                switch (pktSeqNum)
                {
                    case 0:
                        // waiting for 1 but get 0
                        this.NetworkClient.SendNak();
                        break;
                    case 1:
                        // waiting for 1 and get 1
                        this.AppClient.Deliver(pkt.AppData);
                        this.NetworkClient.SendAck();
                        this.CurrentState = ReceiverState.WaitingSenderPacketSeq0;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
```

When an out-of-order packet is received, the receiver sends ACK if it's not corrupted. 

Example

```
+-------------------+-----------------------+---------------------------+---------------------------+-------------------+
| sender app        | sender transport      | connection channel        | receiver transport        | receiver app      |
+-------------------+-----------------------+---------------------------+---------------------------+-------------------+
|                   | seq0 wait for data1   |                           | seq0 wait for sender      | wait for data1    |
|                   | seq0 wait for data1   |                           | seq0 wait for sender      | wait for data1    |
| app data1 ready   | seq0 wait for ACK     | seq0, data1               | seq0 wait for sender      | wait for data1    |
|                   | seq0 wait for ACK     |               seq0, data1 | seq0 wait for sender      | wait for data1    |
|                   | seq0 wait for ACK     |                       ACK | seq1 wait for sender      | data1 delivered   |
|                   | seq0 wait for ACK     | ACK                       | seq1 wait for sender      | wait for data2    |
+-------------------+-----------------------+---------------------------+---------------------------+-------------------+
|                   | seq1 wait for data2   |                           | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for data2   |                           | seq1 wait for sender      | wait for data2    |
| app data2 ready   | seq1 wait for ACK     | seq1, data2               | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     |     seq1, corrupted data2 | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     |                       NAK | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     | NAK                       | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     | seq1, data2               | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     |               seq1, data2 | seq1 wait for sender      | wait for data2    |
|                   | seq1 wait for ACK     |                       ACK | seq0 wait for sender      | data2 delivered   |
|                   | seq1 wait for ACK     | ACK                       | seq0 wait for sender      | wait for data3    |
+-------------------+-----------------------+---------------------------+---------------------------+-------------------+
|                   | seq0 wait for data3   |                           | seq0 wait for sender      | wait for data3    |
|                   | seq0 wait for data3   |                           | seq0 wait for sender      | wait for data3    |
| app data3 ready   | seq0 wait for ACK     | seq0, data3               | seq0 wait for sender      | wait for data3    |
|                   | seq0 wait for ACK     |               seq0, data3 | seq0 wait for sender      | wait for data3    |
|                   | seq0 wait for ACK     |                       ACK | seq1 wait for sender      | data3 delivered   |
|                   | seq0 wait for ACK     | corrupted ACK             | seq1 wait for sender      | wait for data4    |
|                   | seq0 wait for ACK     | seq0, data3               | seq1 wait for sender      | wait for data4    |
|                   | seq0 wait for ACK     |               seq0, data3 | seq1 wait for sender      | wait for data4    |
|                   | seq0 wait for ACK     |                       ACK | seq1 wait for sender      | wait for data4    |
|                   | seq0 wait for ACK     | ACK                       | seq1 wait for sender      | wait for data4    |
+-------------------+-----------------------+---------------------------+---------------------------+-------------------+
|                   | seq1 wait for data4   |                           | seq1 wait for sender      | wait for data4    |
```

We can use ACK0 to replace ACK, ACK1 to replace NAK.

#### Reliable Data Transfer over a Lossy Channel with Bit Errors: rdt3.0

Now consider if the channel lose the packet as well. Solution: wait >= RTT so it's sure that the packet is lost, then retransmit.

So for sender, retransmitting is also implemeted with a **countdown timer**. 

### 3.4.2 Pipelined Reliable Data Transfer Protocols

With timer, performance is poor because it's a stop-and-wait protocol. E.g. US East coast to West coast RTT is 30ms. 

Solution: sender is allowed to send multiple packets without waiting for ACK. This is pipelining:

1.  The range of sequence numbers is increased
2.  The sender and receiver have to buffer more than one packet (`Sender.CachedForRetransmission`)
3.  The above 2 are decided by how does the protocol responds to lost, corrupted, and overly delayed packets. 2 basic approaches: **Go-Back-N** and **selective repeat**.

```
+---------------+---------------+---------------+
| Sender        | 2-Channel     | Receiver      |
+---------------+---------------+---------------+
| 1             |               |               |
|               |               |               |
+---------------+---------------+---------------+
| 2             | 1             |               |
|               |               |               |
+---------------+---------------+---------------+
| 3             | 2   1         |               |
|               |               |               |
+---------------+---------------+---------------+
| 4             | 3   2   1     |               |
|               |               |               |
+---------------+---------------+---------------+
| 5             | 4   3   2   1 |               | first bit of packet 1
|               |               |               |
+---------------+---------------+---------------+
| 6             | 5   4   3   2 | 1             | last bit of packet 1, first bit of packet 2
|               |             1 |               | send ACK 1
+---------------+---------------+---------------+
|               | 6   5   4   3 | 2 1           | last bit of packet 2, first bit of packet 3
|               |         1   2 |               | send ACK 2
+---------------+---------------+---------------+
|               |     6   5   4 | 3 2 1         | last bit of packet 3, first bit of packet 4
|               |     1   2   3 |               | send ACK 3
+---------------+---------------+---------------+
|               |         6   5 | 4 3 2 1       | last bit of packet 4, first bit of packet 5
|               | 1   2   3   4 |               | send ACK 4
+---------------+---------------+---------------+
|               |             6 | 5 4 3 2 1     | last bit of packet 5, first bit of packet 6
|             1 | 2   3   4   5 |               | send ACK 5
+---------------+---------------+---------------+
|               |               | 6 5 4 3 2 1   | last bit of packet 6
|           1 2 | 3   4   5   6 |               | send ACK 6
+---------------+---------------+---------------+
|               |               | 6 5 4 3 2 1   |
|         1 2 3 | 4   5   6     |               |
+---------------+---------------+---------------+
|               |               | 6 5 4 3 2 1   |
|       1 2 3 4 | 5   6         |               |
+---------------+---------------+---------------+
|               |               | 6 5 4 3 2 1   |
|     1 2 3 4 5 | 6             |               |
+---------------+---------------+---------------+
|               |               | 6 5 4 3 2 1   |
|   1 2 3 4 5 6 |               |               |
+---------------+---------------+---------------+
```

### 3.4.3 Go-Back-N (GBN)

In GBN protocol, sender is allowed to send multiple packets without ACK, but is constrained <= N unACKed packets in pipeline. 

```
Sequence Numbers:
            base                    nextseqnum
+-----------+-----------------------+-----------------------+---------------+
| ACKed     | Sent, not yet ACKed   | Usable, not yet sent  | Not usable    |
+-----------+-----------------------+-----------------------+---------------+
| aaaaaaaaa | sssssssssssssssssssss | uuuuuuuuuuuuuuuuuuuuu | nnnnnnnnnnnnn |
+-----------+-----------------------+-----------------------+---------------+
            \_______________________________________________/
                            Window Size N
```

2 seq num pointers: `base` and `nextseqnum`. **Sliding window** with a fixed window size for flow control (TCP congestion control).

```csharp
public class Sender
{
    public int BaseSeqNum;
    public int NextSeqNum;
    public int WindowSize;
    public IndexedQueue<TransportPacket> Window;
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;
    public TransportPacket AckedPlaceHolderSingleton;
    public ITimer InternalTimer;

    // callback
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        var data = e.AppData;

        if (this.NextSeqNum < this.BaseSeqNum + this.WindowSize)
        {
            var pkt = new TransportPacket(
                seqNum: this.NextSeqNum,
                data: data
            );

            // Add this sent packet to buffer
            this.Window.SetByIndex(
                index: this.NextSeqNum - this.BaseSeqNum,
                value: pkt);

            this.NetworkClient.Send(pkt);

            if (this.BaseSeqNum == this.NextSeqNum)
            {
                this.InternalTimer.Start();
            }

            this.NextSeqNum += 1;
        }
        else
        {
            // app process should retry later
            // in real world, this would be buffered or app can only call TcpSend()
            // when the window is not full
            this.AppClient.Reject(data);
        }
    }
    
    public void OnTimeout(object obj, PacketAckTimeoutEventArgs e)
    {
        // retransmit all sent but not ACKed
        for (int i = this.BaseSeqNum; i < this.NextSeqNum; ++ i)
        {
            this.NetworkClient.Send(this.Window.GetByIndex(i));
        }
        this.InternalTimer.Start();
    }

    public void OnAckReceived(object obj, AckReceivedEventArgs e)
    {
        if (e.Ack.IsCorrupted() == false)
        {
            this.BaseSeqNum = e.Ack.SeqNum + 1;
            
            if (this.BaseSeqNum == this.NextSeqNum)
            {
                this.InternalTimer.Stop();
            }
            else
            {
                this.InternalTimer.Start();
            }
        }
    }
}

public class Receiver
{
    public int ExpectedSeqNum = 1;
    public AckPackage LastAck = new AckPackage(seqNum: 0);

    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnPacketReceived(object obj, PacketReceivedEventArgs e)
    {
        TransportPacket pkt = e.Packet;

        if (pkt.IsCorrupted() == false &&
            pkt.SeqNum == this.ExpectedSeqNum)
        {
            this.AppClient.Deliver(pkt.AppData);

            this.LastAck = new AckPackage(seqNum: this.ExpectedSeqNum);
            this.ExpectedSeqNum += 1;
        }

        // send the updated ACK if it's not corrupted
        // all other cases, discards the received packet,
        // resend ACK for last received sequence number
        // to inform the sender that expected is not yet received
        this.NetworkClient.Send(this.LastAck);
    }
}
```

In this GBN protocol, if receiver expects seqnum 10, but gets seqnum 15, though it's needed in future, it would be discarded and 15 should be retransmitted in future. So the data must be consumed **in order**.

Event-based programming. FSM transfers when:

1.  Call from App layer stack `send`, event = `AppDataAvailable`
2.  Timer interrupt
3.  Call from Ip layer `receive`, event = `SegmentAvailable`

Takeaway:

1.  Sequence numbers
2.  Cumulative ACK
3.  Checksum
4.  Timeout/Retransmit operation

### 3.4.4 Selective Repeat (SR)

GBN still has perf problems, when window size * bandwidth-delay is large: a single packet error will cause many packets being retransmitted. so SR retransmits only the possible error or lost packets. 

```
Sender:
            base                    nextseqnum
+-----------+-----------------------+-----------------------+---------------+
| ACKed     | Sent, not yet ACKed   | Usable, not yet sent  | Not usable    |
+-----------+-----------------------+-----------------------+---------------+
| aaaaaaaaa | ss a ssss aaa s a ss | uuuuuuuuuuuuuuuuuuuuu | nnnnnnnnnnnnn |
+-----------+-----------------------+-----------------------+---------------+
            \_______________________________________________/
                            Window Size N
```

range `[base: nextseqnum]`: some are ACKed, some are not. 

Receiver ACKs a received packet (it can be out-of-order). The out-of-order packets are buffered until seqnum < out-of-order packet is received, when the batch can be delivered. 

```csharp
public class Receiver
{
    public int BaseSeqNum;
    public int WindowSize;
    public IndexedQueue<TransportPacket> Window;
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;

    // callback
    public void OnPacketReceived(object obj, PacketReceivedEventArgs e)
    {
        TransportPacket pkt = e.Packet;
        
        if (pkt.IsCorrupted() == false)
        {
            if (this.BaseSeqNum <= pkt.SeqNum && 
                pkt.SeqNum < this.BaseSeqNum + this.WindowSize)
            {
                // the received packet is in the window
                // may or may not send ACK
                if (pkt.SeqNum == this.BaseSeqNum)
                {
                    // Window[base] must be null, so ACK is needed
                    this.NetworkClient.Send(new AckPacket(pkt.SeqNum));

                    // deliver the cached prefix
                    while (this.Window.Peek() != null)
                    {
                        // not-null means this packet has been cached (ACKed) previously
                        TransportPacket q = this.Window.Dequeue();
                        // maintain the window size is not changed
                        this.Window.Enqueue(null);
                        // maintain the pointer
                        this.BaseSeqNum += 1;

                        this.AppClient.Deliver(q.AppData);
                    }

                    return;
                }

                // check if this packet is cached before (position [seqnum])
                int index = pkt.SeqNum - this.BaseSeqNum;
                if (this.Window.GetByIndex(index: index) == null)
                {
                    // cache it
                    this.Window.SetByIndex(index: index, value: pkt);
                    this.NetworkClient.Send(new AckPacket(pkt.SeqNum));
                    return;
                }
                
                // here means the packet is not base and it's cached, no ACK for it
            }
            else if (this.BaseSeqNum - this.WindowSize <= pkt.SeqNum && 
                pkt.SeqNum < this.BaseSeqNum)
            {
                /*  it's in previous window, delivered to application 
                    ACK is still needed even it has been ACKed. 
                    Because the receiver window and sender window may different
                    BUT !! receiver.BaseSeqNum must be in sender's window !!
                    Sender:     a-----------c--------------b
                    Receiver:               c------------------------d
                    the range sender[a : c] may still not receive ACK, 
                    though receiver has sent them.
                    These ACK may lost, sender retransmits these packets [a : c].
                    That's why receiver is receiving them
                    so receiver need to ACK them again for sender's good
                 */
                this.NetworkClient.Send(new AckPacket(pkt.SeqNum));
                return;
            }
        }

        // ignore the packet in all other cases
    }
}

public class Sender
{
    public int BaseSeqNum;
    public int NextSeqNum;
    public int WindowSize;
    public IndexedQueue<TransportPacket> Window;
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;
    public TransportPacket AckedPlaceHolderSingleton;

    // callback
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        var data = e.AppData;

        if (this.NextSeqNum < this.BaseSeqNum + this.WindowSize)
        {
            var pkt = new TransportPacket(
                seqNum: this.NextSeqNum,
                data: data
            );
            pkt.InternalTimer.Start();

            // Add this sent packet to buffer
            this.Window.SetByIndex(
                index: this.NextSeqNum - this.BaseSeqNum,
                value: pkt);

            this.NetworkClient.Send(pkt);
            this.NextSeqNum += 1;
        }
        else
        {
            this.AppClient.Reject(data);
        }
    }

    // callback
    public void OnTimeout(object obj, PacketAckTimeoutEventArgs e)
    {
        // each packet has it's own logical timer
        // so this timeout is only for a single packet
        // retransmit for this packet
        this.NetworkClient.Send(e.Packet);
        e.Packet.InternalTimer.Start();
    }

    // callback
    public void OnAckReceived(object obj, AckReceivedEventArgs e)
    {
        if (e.Ack.IsCorrupted() == false)
        {
            int ackSeqNum = e.Ack.SeqNum;

            this.Window.SetByIndex(
                index: ackSeqNum - this.BaseSeqNum, 
                value: this.AckedPlaceHolderSingleton);
            
            while (this.Window.Peek() == this.AckedPlaceHolderSingleton)
            {
                // so the window size is not changed
                this.Window.Dequeue();
                this.Window.Enqueue(null);
                this.BaseSeqNum += 1;
            }
        }
        // for corrupted ACK, receiver has delivered app data
        // but sender will wait for timeout and retransmit
        // so the retransmitted package may be in [a : c]
        // receiver resend ACK for it
    }
}
```

Real world: packets may be reordered.

## 3.5 Connection-Oriented Transport: TCP

# Chapter 4 The Network Layer

# Chapter 8 Security in Computer Networks