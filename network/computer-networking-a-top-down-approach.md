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

RFC 793, RFC 1122, RFC 1323, RFC 2018, and RFC 2581

### 3.5.1 The TCP Connection

Connection-oriented: before transfer data, 2 processes must first handshake with each other to send preliminary segments to each other to establish parameters.

TCP only runs in end system, intermediate network do not maintain TCP connection state.

**Full-duplex Service**:  A <---> X, B <---> X ===> A <---> B

**Point-to-point**: single sender & single receiver.

RFC793 did not specify the delivery timing. The max amount of data can be placed in segment is limited by _maximum segment size (MSS)_, determined by largest link-layer frame.

Takeaway: TCP contains:

1.  Buffers (sender role and receiver role)
2.  Variables
3.  Socket connection to a process in one host
4.  These TCP states does not exist in network layer (routers, switches, repeaters)


### 3.5.2 TCP Segment Structure

```cpp
typedef struct
{
    // the source port number
    uint16_t    source_port_number;
    // the destination port number
    uint16_t    destination_port_number;

    // the sequence number for reliable data transfer
    uint32_t    sequence_number;
    // the ack number for reliable data transfer
    uint32_t    acknowledgement_number;
    
    // the header length in 32-bit words. Due to option fields,
    // the header length can varies, <= 16 * 32bits = 64Bytes
    // typically, options are empty so 20 bytes, record 5
    uint4_t     header_length;

    // unused
    uint8_t     unused: 6;

    // flags total 6 bits

    // if there is data that sending app data as urgent
    uint8_t     urg_bit: 1;
    // if the value caried in acknowledgement_number is valid
    uint8_t     ack_bit: 1;
    // if the receiver should pass data to app layer immediately
    uint8_t     psh_bit: 1;
    // rst, sync, fin are used for connection setup and teardown
    uint8_t     rst_bit: 1;
    uint8_t     syn_bit: 1;
    uint8_t     fin_bit: 1;

    // used for flow control
    uint16_t    receiver_window;

    // the checksum
    uint16_t    internet_checksum;

    // pointer to the urgent data, cowork with urg_bit
    uint16_t    urgent data pointer;

    uint32_t    options;
}
```

#### Sequence Numbers and Acknowledgment Numbers

Most critical fields: sequence number and ack number, for reliable data transfer service.

Data in TCP: unstructured, ordered, stream of bytes. _SeqNum for a segment_ is the byte-stream number of the first byte in segment.

Example: send 500,000 bytes, MSS = 1,000 bytes ==> 500 segments.

```
segment[0].seqNum = 0
segment[1].seqNum = 1,000
segment[2].seqNum = 2,000
...
```

ACK number Example:

Host A received all bytes seqNum `0 : 535` from B, waiting for byte `536`. A is going to send a new segment, send `ackNum: 536` to B.

Another example. A received byte `[0, 535]` and `[900, 1000]` from B, but not `[536, 899]`. A waiting for `536`. Then A's next segment to B will contains `ackNum: 536` to B so B knows `[0, 535]` is received. 

**Cumulative Acknowledgement**: TCP only ACKs bytes up to the first missing byte in the stream.

And for the out-of-order segment `[900, 1000]`, RFC has not defined required actions for it. 2 choices:

1.  Discard the out-of-order segment
2.  Receiver maintains a buffer and waiting for the missing bytes. This is taken in practice to reduce network bandwidth.

Initial seqnum is randomly selected to reduce collision.

### 3.5.3 Round-Trip Time Estimation and Timeout

TCP use timeout/retransmit to recover from lost segments. Timeout > RTT. So TCP needs to estimate RTT.

```
SampleRTT = T(ACK of seg[i] is received) - T(seg[i] is sent)
```

Only compute `SampleRtt` for segments that have been transmitted once. Use `SampleRtt` to calculate `EstimatedRtt` (weighted average):

```
EstimatedRtt = (1 - a) * EstimatedRtt + a * SampleRtt
```

`a=0.125` is recommended. 

And the variability of RTT:

```
DevRtt = (1 - b) * DevRtt + b * ABS(SampleRtt - EstimatedRtt)
```

`b = 0.25` is recommanded.

With expectation and variability, timeout interval is calculated as:

```
TimeoutInterval = EstimatedRtt + 4 * DevRtt
```

The init value of `TimeoutInterval` is 1 second. When timeout, double the timeout interval to avoid premature timeout. When the segment is received, compute the interval using the formula above.

### 3.5.4 Reliable Data Transfer

TCP builds reliability on top of IP. 

RFC 6298 recommends use **only one retransmission timer** even there are multiple unACKed segments.

Simplified TCP sender:

```csharp
public class Sender
{
    public int BaseSeqNum;
    public int NextSeqNum;
    public int WindowSize;
    public Queue<TransportPacket> UnAcked;
    public INetworkLayerClient NetClient;
    public IApplicationLayerClient AppClient;
    public TransportPacket AckedPlaceHolderSingleton;
    public ITimer InternalTimer;

    // callback
    public void OnAppDataReceived(object obj, AppDataReceivedEventArgs e)
    {
        var data = e.AppData;
        int m = this.NextSeqNum + data.ByteSize;

        if (m < this.BaseSeqNum + this.WindowSize)
        {
            var pkt = new TransportPacket(
                seqNum: this.NextSeqNum,
                data: data
            );

            if (this.InternalTimer.IsStopped())
            {
                // so the timer works for the oldest packet
                this.InternalTimer.Start();
            }

            // Add this sent packet to buffer
            this.UnAcked.Enqueue(pkt);

            this.NetworkClient.Send(pkt);
            this.NextSeqNum = m;
        }
        else
        {
            this.AppClient.Reject(data);
        }
    }

    // callback
    public void OnTimeout(object obj, PacketAckTimeoutEventArgs e)
    {
        if (this.UnAcked.IsEmpty() == false)
        {
            // retransmit the oldest
            this.NetworkClient.Send(this.UnAcked.Peek());
            this.InternalTimer.Start();            
        }
    }

    // callback
    public void OnAckReceived(object obj, AckReceivedEventArgs e)
    {
        if (e.Ack.IsCorrupted() == false)
        {
            int ackNum = e.AckNum;

            if (ackNum > this.BaseSeqNum)
            {
                this.BaseSeqNum = ackNum;

                while (this.UnAcked.Peek().SeqNum < ackNum)
                {
                    // ACKed
                    this.UnAcked.Dequeue();
                }
            }

            if (this.UnAcked.IsEmpty() == false)
            {
                this.InternalTimer.Start();
            }
        }
    }
}
```

A Few Interesting Scenarios

Case 1

```
Host A:
1.  Send seq = 92, data size = 8
    waiting for ACK = 100

2.  seq = 92 timeout
    Resend seq = 92, data size = 8
    waiting for ACK = 100

3.  Receive ACK = 100
```

In this case, when timeout, Host A thinks: (1) Segment 92 is lost or (2) ACK 100 is lost. Retransmit segment 92.

Case 2

```
Host A:
1.  Send seq = 92, data size = 8
    waiting for ACK = 100

2.  Send seq = 100, data size = 20
    waiting for ACK = 120

3.  seq = 92 timeout
    Resend seq = 92, data size = 8
    waiting for ACK = 120 (120 > 100)
    Restart seq = 92 timer

4.  Receive ACK = 120

---------------------------------------------

Host B:
1.  Receive seq = 92, data size = 8
    send ACK = 100 (92 + 8)

2.  Receive seq = 100, data size = 20
    send ACK = 120 (100 + 20)

3.  Receive seq = 92, data size = 8
    resend ACK = 120 (120 > 100)
```

When host A receives ACK 120 in second timeout window, knows that seq=100 and seq=120 are all received (100 < 120).

When host B receives seq=92 again, B knows ACK=100 is lost. But it has sent ACK=100 and ACK=120, so resend ACK=max(120, 100).

Case 3

```
Host A:
1.  Send seq = 92, data size = 8
    seq = 92 timer starts
    waiting for ACK = 100

2.  Send seq = 100, data size = 20
    waiting for ACK = 120

3.  Receive ACK = 120

4.  seq = 92 timeout

----------------------------------

Host B:

1.  Receive seq = 92, data size = 8
    send ACK = 100

2.  Receive seq = 100, data size = 120
    send ACK = 120
```

Host A in step 3 knows the 100, 120 are both ACKed, but `ACK = 100` may lost.

#### Fast Retransmit

Avoid waiting for timeout to find that the packet is lost. **Duplicated ACK**:

```
Host A:

1.  Send seq = 92, data size = 8
    waiting for ACK = 100

2.  Send seq = 100, data size = 20
    waiting for ACK = 120

3.  Send seq = 120, data size = 15
    waiting for ACK = 135

4.  Send seq = 135, data size = 6
    waiting for ACK = 141

5.  Send seq = 141, data size = 16
    waiting for ACK = 157

6.  Receive ACK = 100
    seq = 100 timer starts

7.  Receive ACK = 100   // 1st duplicate, host A knows seq = 100 may be lost or in the way

8.  Receive ACK = 100   // 2nd duplicate

9.  Receive ACK = 100   // 3rd duplicate, fast retransmit
    Resend seq = 100, data size = 20

10.  seq = 100 timeout

-----------------------------------

Host B:

1.  Receive seq = 92, data size = 8
    Send ACK = 100

2.  Receive seq = 120, data size = 15
    Send ACK = 100
    // receiver finds a hole [100 : 119]

3.  Receive seq = 120, data size = 15
    Send ACK = 100

4.  Receive seq = 132, data size = 6
    Send ACK = 100

4.  Receive seq = 141, data size = 16
    Send ACK = 100
```

RFC 5681:

**When**: Arrival of in-order segment with expected sequence number. All data up to expected sequence number already acknowledged.

**Receiver**: Delayed ACK. Wait up to 500 msec for arrival of another in-order segment. If next in-order segment does not arrive in this interval, send an ACK.

**When**: Arrival of in-order segment with expected sequence number. One other in-order segment waiting for ACK transmission.

**Receiver**: Immediately send single cumulative ACK, ACKing both in-order segments.

**When**: Arrival of out-of-order segment with higher-than-expected sequence number. Gap detected.

**Receiver**: Immediately send duplicate ACK, indicating sequence number of next expected byte (which is the lower end of the gap).

**When**: Arrival of segment that partially or completely fills in gap in received data.

**Receiver**: Immediately send ACK, provided that segment starts at the lower end of gap.

When receiver finds a missing segment (`[A+1,B-1]`) in the data stream, receiver send the ACK for the last in-order byte of data (`A`):

```
                    A        B
seq num: xxxxxxxxxxxx________xxxxxxxx
```

Fast retransmit: resend the missing segment before that segment's timer expires. 

TCP's error-recovery is a hybrid of GBN and SR.

### 3.5.5 Flow Control

Each side of TCP maintains a buffer to reorder the packets. Sender may overflow the receiver's buffer -- **Flow-Control Service** -- match the 2 speeds:

1.  Sender's sending speed
2.  App's reading speed

**Congestion Control**: sender may be throttled within IP network. Different flow control.

Suppose receiver discards out-out-order segments.

Use _receiver window_ to do flow control. With this from TCP header, sender can know how much free buffer space is still available at receiver.

```
Data flow from IP

+===============+ \-----------------\   
|               |  |                 |  rwnd = RcvBuffer -
| usable space  |  | receive window  |      (LastByteRcvd - LastByteRead)
|               |  | rwnd            |
+---------------+ /                  |  <---- LastByteRcvd
|               |                    |
| TCP used      |                    | 
|               |                    |
+===============+  -----------------/   <---- LastByteRead
                    receive buffer
                    RcvBuffer
Application Process
```

So we have

```
rwnd := RcvBuffer - (LastByteRcvd - LastByteRead) >= 0
```

Initially, `rwnd := RcvBuffer`. 

Sender keeps 2 variables: `LastByteSent, LastByteAcked`. So `LastByteSent - LastByteAcked` is the size of unAcked data. We have: `LastByteSent - LastByteAcked` <= the packets receiver will receive (packet may be lost) = receiver will ACKed. 

If:

```
LastByteSent - LastByteAcked <= rwnd
```

Then the buffer will not overflow.

But one problem: Host B's receive window is `0`, host A now waiting the receive window become positive. But if B do not send packet to A, even the receive buffer is emptied, host A will not know, host A will be blocked.

So host A needs to send _one byte_ when B's receive window is zero. 

UDP does not have flow control: a finite-sized buffer. Just drop when overflow.

### 3.5.6 TCP Connection Management

3-way handshake

1.  client sends a special segment to server. Set `syn_bit = 1`, so it's named `SYN` segment. And a random initial seq number. 
    ```csharp
    new TcpSegment (
        SYN: 1, 
        SeqNum: client_isn
    )
    ```
2.  server extracts `SYN` segment from datagram, allocates TCP buffers and variables for connection. Then server send `SYNACK` segment:
    ```csharp
    new TcpSegment(
        SYN: 1, 
        AckNum: client_isn + 1,
        SeqNum: server_isn,
    )
    ```
3.  client receives `SYNACK` from server, client allocates buffer & variables. client sends:
    ```csharp
    new TcpSegment(
        SYN: 0, 
        AckNum: server_isn + 1,
        SeqNum: client_isn + 1,
        Data: appData
    )
    ```

The following segments will set `SYN = 0`.

4-way closing

```
Client (ESTABLISHED):

1.  Closes the connection. Send FIN to server, ESTABLISHED --> FIN_WAIT_1

2.  Receives ACK for FIN, do nothing, FIN_WAIT_1 --> FIN_WAIT_2, waiting for server's FIN

3.  Receives FIN from server, send ACK for this FIN, FIN_WAIT_2 --> TIME_WAIT, wait for 30 seconds

4.  Time to 30 seconds, TIME_WAIT --> CLOSED

---------------------------------------

Server (ESTABLISHED):

1.  Receives FIN from client, send ACK, ESTABLISHED --> CLOSE_WAIT, do the closing work

2.  Close the connection, send FIN to client, waiting for client's ACK for this FIN, CLOSE_WAIT --> LAST_ACK

3.  ACK received, LAST_ACK --> CLOSED
```

When host receives segment with destination port 80, while host is not accepting connections on port 80, host will send segment with `RST` flag back. 

## 3.6 Principles of Congestion Control

**Available Bit-Rate (ABR)** service in **Asynchronous Transfer Mode (ATM)** networks.

### 3.6.1 The Causes and the Costs of Congestion

```
       R_in                            R_out
Host A      Host B              Host C      Host D
  |           |                   |           |
  +-----+-----+                   +-----+-----+
        |            Buffer             |
        +----------> Router ----->------+
                            Link Capacity (outgoing)
```

Things to consider:

1.  Finite buffer in router
2.  Finite link capacity
3.  Multihop of routers

Bad things:

1.  Queuing delays is large when packet-arrival speed nears link capacity;
2.  Buffer overflow will cause packet retransmission;
3.  Router will use small bandwidth to forward unneeded copies due to unneeded retransmission (case 2);
4.  When a packet is dropped along a path, the transmission capacity that was used at each of the upstream links to forward that packet to the point at which it is dropped ends up having been wasted.

### 3.6.2 Approaches to Congestion Control

2 approaches to congestion control at boardcast level:

1.  End-to-end congestion control. Network does not provide support for transport layer. (TCP's way)
2.  Network-assisted congestion control. E.g., router provides feedback to sender. 

## 3.7 TCP Congestion Control

Each sender limits the sending traffic when congestion is detected. When finds congestion (sender to receiver path) is small, increases the sending rate; else, reduces it. 

3 questions: (1) how to limit; (2) how to detect; (3) how to adujust.

**Q1: how to limit**

Keep a **congestion window** (`cwnd`): limit the sender's traffic into network:

```
LastByteSent - LasytByteAcked <= min{cwnd, rwnd}
```

The sent but not ACKed bytes is no greater than `min{cwnd, rwnd}`. So the sending speed is aroud `min{cwnd, rwnd}/RTT`. Thus by adjusting `cwnd`, we can control the sending rate.

**Q2: how to detect**

Loss event: when (1) timeout; (2) 3-duplicated-ACKs, so a packet is lost. In congestion, router drops the packet from buffer, making this loss event.

Self-Clocking: sender uses ACK rate to increase window size.

**Q3: how to adjust**

Trade-off: network congestion vs bandwidth usage

1.  A lost segment implies congestion, so the sending rate should decrease when a segment is lost;
2.  An ACK means the network is capable to carry more segments, so increase
3.  Bandwidth probing: keep retrying to reach the bandwidth limit.

ACKs and loss segments are all implicit signals.

**TCP Congestion-Control Algorithm**: (1) slow start; (2) congestion avoidance; (3) fast recovery

#### Slow Start

`cwnd` is small at beginning of connection, 1 MSS [RFC 3390]. So the initial sending rate is around `MSS/RTT = 500Bytes/200ms = 20KBPS`. 

For each first ACK, when in _slow start_ state, `cwnd` increases 1 MSS. 

```
Sender:

1.  seq = 0, data size = 1 MSS
        cwnd = 1 MSS

2.  seq = 0 ACK
        cwnd = 1 + 1 MSS
        send 1 + 1 MSS data (2 segments):
            seq = 1 MSS, data size = 1 MSS
            seq = 2 MSS, data size = 1 MSS

3.  seq = MSS ACK
        cwnd = 2 + 1 = 3 MSS
        send 3 MSS data (3 segments):
            seq = 3 MSS, data size = 1 MSS
            seq = 4 MSS, data size = 1 MSS
            seq = 5 MSS, data size = 1 MSS

4.  seq = MSS ACK
        cwnd = 3 + 1 = 4 MSS
        send 4 MSS data (4 segments):
            seq = 1 MSS, data size = 1 MSS
            seq = 2 MSS, data size = 1 MSS
            seq = 3 MSS, data size = 1 MSS
            seq = 4 MSS, data size = 1 MSS
```

Ordered by ACKed segment index, num of childs increases in BFS order:

```
[0]=1 MSS When seq=0 is ACKed, it's having 1 MSS cwnd
 |
[1]=2 MSS
 |
 +---------------------------+
 |                           |
[2]=3 childs                [3]=4 childs
 |                           |
 +-------+-------+           +-------+-------+-------+
 |       |       |           |       |       |       |
[4]=5   [5]=6   [6]=7       [7]=8   [8]=9   [9]=10  [10]=11
```

So `cwnd` grows slow first but fast with time (exponentional). 3 cases to stop fast start:

1.  Packet timeout, so a loss. Cut `cwnd` to half: `cwnd/2`. 
2.  `cwnd` reaches a threshold `ssthresh`. `ssthresh` is the half of last congestion `cwnd` value. Switches to **congestion avoidance** state.
3.  Three-duplicated-ACKs, packet loss. TCP performs fast retransmit and switches to **fast recovery** state. 

#### Congestion Avoidance

TCP increase `cwnd` one MSS each RTT. Linar to time. To implement this, for each ACK, `cwnd += MSS * MSS / cwnd`. 

When three-duplicated-ACKs, packet loss. So threshold to half, linearly increase `cwnd`, to **Fast recovery**.

#### Fast Recovery

Linear increase, 1 MSS for each duplicate ACK. When ACK for the missing segment arrives, switch to congestion-avoidance.

#### TCP Congestion Control: Retrospective

Additive-Increase, Multiplicative-Decrease (AIMD)

Three-duplicated-ACKs are often the packet loss, fast recovery. 

So the `cwnd-time` curve will fluctuate around bandwidth. 

FSM

# Chapter 4 The Network Layer

## 4.5 Routing Algorithms

The network must determine the path that packets take from senders to receivers through the network of routers. Typically the host is attached directly to one router, **default router/first-hop router**. So a packet from a host is transferred to default router. 

The routing algorithm: given a set of routers with links connecting the routers, a routing algorithm shuold find a good path (least cost) from source router to destination router. 

-   **Global Routing Algorithms**: compute the least-cost path based on the complete, global knowledge about the network. Take the whole graph/topology as input. *Linke-state (LS) algorithms*
-   **Decentralized Routing Algorithm**: the calculation is carried out in an iterative, distributed manner. Each node only knows the neighborhood.E.g., *Distance-Vector (DV) algorithm*.

Another classification is if they are load-sensitive or load-insensitive.

### 4.5.1 The Link-State (LS) Routing Algorithm

The complete topology is known and taken as input. Each node boardcast link-state packets to all other nodes in the network, containing the identities & costs of links, accomplished by a **Link-State Broadcast Algorithm**.

*Dijkstra's Algorithm* as link-state routing algorithm to compute the least-cost path from source node to all other nodes in the network. After $k$ iterations, will know least-cost path to $k$ destinations.

```cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Routing
{
    public class Node
    {
        /// <summary>
        /// The edges with this node and edge cost
        /// </summary>
        public Dictionary<Node, int> Edges = new Dictionary<Node, int>();

        /// <summary>
        /// The cost of the shortest path to destination
        /// </summary>
        public Dictionary<Node, int> DistTo = new Dictionary<Node, int>();

        /// <summary>
        /// The shortest path to destination
        /// </summary>
        public Dictionary<Node, List<Node>> PathTo = new Dictionary<Node, List<Node>>();

        /// <summary>
        /// The name
        /// </summary>
        public string Name;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of node</param>
        public Node(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Calculate the shortest path from this node to every node in graph
        /// </summary>
        /// <param name="graph">The graph topology</param>
        public void CalculateUnDirectedShortestPath(HashSet<Node> graph)
        {
            // Add the node itself to finished. Deep copy
            HashSet<Node> notFinished = new HashSet<Node>(graph);
            notFinished.Remove(this);
            this.PathTo[this] = null;

            // initialize path
            foreach (Node v in notFinished)
            {
                this.PathTo[v] = new List<Node>() { v };
            }

            // infinite as unknown and neighbors are currently known
            foreach (Node v in graph)
            {
                if (this.Edges.TryGetValue(v, out int cost))
                {
                    this.DistTo[v] = cost;
                }
                else
                {
                    this.DistTo[v] = Int32.MaxValue;
                }
            }
            this.DistTo[this] = 0;

            // calculate and converge
            while (notFinished.Count > 0)
            {
                // not converged

                // find the shortest path of unfinished node
                Node w = (from t in notFinished
                    select new KeyValuePair<Node, int>(t, this.DistTo[t]))
                    .OrderBy(x => x.Value).First().Key;
                notFinished.Remove(w);

                foreach (Node v in w.Edges.Keys)
                {
                    if (this.DistTo[v] > this.DistTo[w] + w.Edges[v])
                    {
                        // update distance
                        this.DistTo[v] = this.DistTo[w] + w.Edges[v];

                        // update path
                        this.PathTo[v] = new List<Node>(this.PathTo[w]);
                        this.PathTo[v].Add(v);
                    }
                }
            }
        }
    }
}
```

### 4.5.2 The Distance-Vector (DV) Routing Algorithm

### 4.5.3 Hierarchial Routing

# Chapter 8 Security in Computer Networks