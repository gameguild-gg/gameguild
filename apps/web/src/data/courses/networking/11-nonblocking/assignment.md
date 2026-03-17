# Final Project — Checkpoint 2: Architecture Design

**Due:** Sunday, 2026/03/29

::: warning

Late submissions incur a **1% penalty per day** up to a maximum of 25%. See the [Final Project](../09-break/finalproject.md) page for full details.

:::

## Overview

This week you will design and document the architecture of your networked application. The deliverable is a **network protocol design document** with an accompanying **architecture diagram** showing every major component, data flow, and how the networking integrates with the application.

## Deliverables

Submit a document or diagram that includes:

1. **System Diagram** — A visual representation of your network architecture. Show clients, servers (or peers), their responsibilities, and how they communicate. Recommended tools:
   - [Mermaid](https://mermaid.live/)
   - [draw.io](https://draw.io/)
   - [Excalidraw](https://excalidraw.com/)

2. **Component Breakdown** — For each major component (client, server, relay, database, etc.), write 2–3 sentences describing its purpose, inputs, and outputs.

3. **Message Format Specification** — Define the messages your application will exchange. For each message type, describe:
   - Message ID / type identifier
   - Fields and their data types
   - Serialization format (binary, JSON, Protobuf, etc.)
   - Direction (client→server, server→client, peer→peer)

4. **Data Flow** — Show how application state flows through the network. Include connection lifecycle (handshake, authentication, gameplay, disconnection) and any state synchronization strategy.

5. **Feasibility Notes** — Identify the riskiest part of your implementation and describe how you plan to address it. What will you prototype first?

::: tip

If you are unsure about your architecture, look at the case studies from class and readings: Valve Source Multiplayer Networking, Overwatch Netcode, Age of Empires lockstep, and Gaffer on Games articles.

:::

## Grading Rubric

| Criterion                  | Points  |
| -------------------------- | :-----: |
| System diagram             |   30    |
| Component breakdown        |   25    |
| Message format & data flow |   25    |
| Feasibility analysis       |   20    |
| **Total**                  | **100** |

## Submission

Submit as a link (shared Mermaid/draw.io/Google Doc) or PDF via the course submission form.
