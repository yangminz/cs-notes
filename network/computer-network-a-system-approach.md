4th By Larry L. Peterson and Bruce S. Dave

# Chapter 3 Packet Switching

Problem: Not all networks are directly connected. Enable communication between the hosts that are not directly connected. Like telephone network: need switch to create the illusion.

Packet switch: a device with inputs and outputs to switch interconnects. For a input from some port, determine the right output to go for the packet.

A key problem: finite bandwidth of putputs. If too much, the problem of contention. When discarded too many packets, congested.

2 technologies: 

1.  LAN switching, from Ethernet bridging.
2.  Asynchronous transfer mode (ATM), used in wide area networks.

*Address schema + routing algorithm = LAN.*

## 3.2 Bridges and LAN Switches

Switch forwards packets between LANs such as Ethernets due to 2500m physical limitation. So a node in-between to forward frames, named **Bridge**. `LAN -- Bridge -- LAN` = extended LAN.

### 3.2.1 Learning Bridges

Bridge needs to learn where (output ports) to forward the packets. Host boardcasts the packet to every switch, while only the receiver server responses. 

Learning Bridges: Every switch maintains a table: `MAC Address -> Switch Port` - Switch learns: If you want to send a packet to this MAC address, send it on this port.

`A` sends a packet to bridge on port `2`, record `[A, 2]`. Now `B` wants to send to `A`, forward the packet to port `2`.

Pros: Self-Organizing. Hardware and algorithms are fairly simple. Each switch maintains `O(#Hosts)` states.

Cons: **Loops** or **Boardcast Storm** - boardcast will never die and will accumulate. Use Spanning Tree Algorithm to reduce the topology graph to a tree while all LAN segments are still connected to the LAN.

### 3.2.2 Spanning Tree Algorithm

Loops may be built into the network to provide redundancy in case of failure.

Make the graph to a tree to reduce loops. All segments are still connected. Implemented by bridges/switches ignoring ports (removing edges). When node/edge fails, rerun to converge to new tree.

Bridge/Switch/Router with lowest MAC address is tree root. Each bridge remembers the port that is on the shortest path to the root. So every switch maintains a data structure: `(Root, PathLength, NextHop)`. 

When the switches do not receive message any more, the protocol has converged. Not necessarily the shortest path.

*Trade-Off of the algorithms*

-   **Resilience**: The ability to provide and maintain an acceptable level of service in the face of faults and challenges to normal operation.
-   **Fully Distributed**: Does not assume the previous existence of a central coordinator.
-   **State**: The amount of memory each node uses.
-   **Convergence**: The process of routers agreeing on optimal routes for forwarding packets and thereby completing the updating of their routing table.

Boardcast + Learning Bridges

-   Resilience: Packets are can be received if the route exists
-   Fully Distributed: Yes
-   State per Node: O(Nodes)
-   Convergence: No setup time
-   Routing Efficiency: Boardcast storms
-   Shortest Path: Not necessarily

Boardcast + Learning Bridges + Distributed Spanning Tree

-   Resilience: Need to recompute spanning tree if failure
-   Fully Distributed: Yes
-   State per Node: O(Nodes) + O(Tree Height)
-   Convergence: Run spanning tree protocol before routing
-   Routing Efficiency: Still sends new connections everywhere
-   Shortest Path: Not necessarily

Distance Vector

-   Resilience: Long converge time when link weights increase or when links go down
-   Fully Distributed: Yes
-   State per Node: `O(Nodes) * max{Node Degree}`
-   Convergence: Need to run DV before routing. `O(max{Best Path})`
-   Routing Efficiency: Packets sent directly to their destination
-   Shortest Path: Yes

# Chapter 4 Internetworking

## 4.2 Routing

### 4.2.3 Link State (OSPF)
