By W. Richard Stevens, Bill Fenner, Andrew M. Rudoff

# Chapter 1. Introduction

Web server: long running program

Web client: initiates the communication with the server

Note that the actual flow of information between the client and server goes down the protocol stack on one side, across the network, and up the protocol stack on the other side. Also note that the client and server are typically user processes, while the TCP and IP protocols are normally part of the protocol stack within the kernel.

## 1.2 A Simple Daytime Client

### Read and display server’s reply

We must be careful when using TCP because it is a byte-stream protocol with no record boundaries. 

With a byte-stream protocol, these 26 bytes can be returned in numerous ways: a single TCP segment containing all 26 bytes of data, in 26 TCP segments each containing 1 byte of data, or any other combination that totals to 26 bytes.

when reading from a TCP socket, we always need to code the read in a loop and terminate the loop when either read returns 0 (i.e., the other end closed the connection) or a value less than 0 (an error).

The important concept here is that TCP itself provides no record markers: If an application wants to delineate the ends of records, it must do so itself and there are a few common ways to accomplish this.

## Chapter 2. The Transport Layer: TCP, UDP, and SCTP

### User Datagram Protocol (UDP)

If we want to be certain that a datagram reaches its destination, we can build lots of features into our application: acknowledgments from the other end, timeouts, retransmissions, and the like.

Each UDP datagram has a length. The length of a datagram is passed to the receiving application along with the data. We have already mentioned that TCP is a byte-stream protocol, without any record boundaries at all (Section 1.2), which differs from UDP.

### Transmission Control Protocol (TCP)

TCP does not guarantee that the data will be received by the other endpoint, as this is impossible. It delivers data to the other endpoint if possible, and notifies the user (by giving up on retransmissions and breaking the connection) if it is not possible. Therefore, TCP cannot be described as a 100% reliable protocol; it provides **reliable delivery of data** or **reliable notification of failure**.