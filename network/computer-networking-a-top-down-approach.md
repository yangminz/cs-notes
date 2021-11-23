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

# Chapter 4 The Network Layer

# Chapter 8 Security in Computer Networks