// ===== LaserNode.cs =====
using System.Collections.Generic;
using UnityEngine;


// Central manager: triggers propagation and holds FX prefabs


// ===== LaserBeam.cs =====
using UnityEngine;



// ===== LaserRenderer.cs =====


// ===== SegmentUtility.cs =====


// ===== Notes =====
// - Attach Generator/Connector/Receiver scripts to corresponding prefabs in the scene.
// - Set 'connections' at runtime by the player's interaction code (ConnectTo/DisconnectAll API).
// - Place LaserGraphManager on a singleton GameObject and assign FX prefabs and blockingLayers (including Player layer).
// - This implementation focuses on node-based propagation, segment-segment detection and simple receiver activation.
// - Edge cases like "exact middle collision by connector count" are approximated by the algorithm: when beams of different colors meet on the same segment they spark and are trimmed.
// - You can extend the PropagateFromGenerator method to compute exact "mid-chain" collisions by building full paths and comparing distances in node-count metric.
