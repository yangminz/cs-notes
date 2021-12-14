3rd By Jonathan Corbet & Alessandro Rubini

# Chapter 17. Network Drivers

3 kinds of devices:

1.  Char driver
2.  Block driver
3.  Network driver

Network interface like mounted block device: transmit & receive blocks on request. Difference: disk is a special file in `/dev` directory, network has no such entry point. And _everything is a file_ does not apply to network.

Most important diff: block drivers operate only in resposne to requests from kernel, while network drivers receive packets asynchronously from outside. 

Network subsystem is designed to be completely protocol-independent. Hide protocol details from kernel.

Memory-based modularized network interface, **snull**. To make it simple, snull uses Ethernet hardware protocol and transmits IP packets.

## The `net_device` Structure in Detail

The `net_device` is at the very core of network driver layer. 

## Packet Transmission

Send a packet over a network link. When kernel needs to send a data packet, call driver's `hard_start_transmit` method to put data on an **outgoing queue**.

Packets handled by the kernel is contained in a socket buffer: `struct sk_buff`, `<linux/skbuff.h>`. The I/O buffers of any socket are lists of this structure.

? The same `sk_buff` structure is used to host network data throughout all the Linux network subsystems, but a socket buffer is just a packet as far as the interface is concerned.

`skb = &sk_buffer`

The socket buffer is very complex. `skb->data` the packet being transmitted, `skb->len` the length of bytes. 

```cpp
int snull_tx(struct sk_buff *skb, struct net_device *dev)
{
    int len;
    char *data, shortpkt[ETH_ZLEN];
    struct snull_priv *priv = netdev_priv(dev);
    
    data = skb->data;
    len = skb->len;
    if (len < ETH_ZLEN) {
        memset(shortpkt, 0, ETH_ZLEN);
        memcpy(shortpkt, skb->data, skb->len);
        len = ETH_ZLEN;
        data = shortpkt;
    }
    dev->trans_start = jiffies; /* save the timestamp */

    /* Remember the skb, so we can free it at interrupt time */
    priv->skb = skb;

    /* actual deliver of data is device-specific, and not shown here */
    // hardware related transmission function
    snull_hw_tx(data, len, dev);

    // driver has taken responsibility for the packet, 
    // should make its best effort to ensure that transmission succeeds
    // must free skb at the end
    return 0; /* Our simple device can not fail */
}
```

### Controlling Transmission Concurrency

### Transmission Timeouts

### Scatter/Gather I/O

## Packet Reception

2 modes of packet recveption can be implemented by network drivers:

1.  Interrupt driven - most drivers
2.  Polling - drivers for high-bandwidth adapters

```cpp
void snull_rx(struct net_device *dev, struct snull_packet *pkt)
{
    struct sk_buff *skb;
    struct snull_priv *priv = netdev_priv(dev);

    /*
     * The packet has been retrieved from the transmission
     * medium. Build an skb around it, so upper layers can handle it
     * 
     * Calls kmalloc with atomic priority, so it can be used safely at interrupt time
     */
    skb = dev_alloc_skb(pkt->datalen + 2);
    if (!skb) {
        if (printk_ratelimit(  ))
            printk(KERN_NOTICE "snull rx: low on mem - packet dropped\n");
        priv->stats.rx_dropped++;
        goto out;
    }
    // copy data into socket buffer
    // skb_put: update end-of-data pointer in the buffer and return to the newly created space
    memcpy(skb_put(skb, pkt->datalen), pkt->data, pkt->datalen);

    /* Write metadata, and then pass to the receive level */
    skb->dev = dev;
    skb->protocol = eth_type_trans(skb, dev);
    skb->ip_summed = CHECKSUM_UNNECESSARY; /* don't check it */
    priv->stats.rx_packets++;
    priv->stats.rx_bytes += pkt->datalen;
    netif_rx(skb);
  out:
    return;
}
```



