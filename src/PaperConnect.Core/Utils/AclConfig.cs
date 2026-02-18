using System;
using System.Collections.Generic;
using System.Text;
using Tomlyn;
using Tomlyn.Model;  // 需要此命名空间
namespace PaperConnect.Core.Utils
{
	// 枚举定义（与 protobuf 中的数值对应）
	public enum Action
	{
		Allow = 1,
		Drop = 2
		
	}

	public enum Protocol
	{
		Tcp = 1,
		Udp = 2
		// 其他值按需添加
	}

	public enum ChainType
	{
		Inbound = 1,
		Outbound = 2
	}

	// 消息结构定义
	public class Rule
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public uint Priority { get; set; }
		public bool Enabled { get; set; }
		public int Protocol { get; set; }               // Protocol 枚举的 int 值
		public List<string> Ports { get; set; }
		public List<string> SourceIPs { get; set; }
		public List<string> DestinationIPs { get; set; }
		public List<string> SourcePorts { get; set; }
		public List<int> AppProtocols { get; set; }
		public List<string> PayloadPrefixHex { get; set; }
		public uint? PayloadMinLen { get; set; }
		public uint? PayloadMaxLen { get; set; }
		public bool? DstIsBroadcast { get; set; }
		public bool? DstIsMulticast { get; set; }
		public int Action { get; set; }                 // Action 枚举的 int 值
		public uint RateLimit { get; set; }
		public uint BurstLimit { get; set; }
		public bool Stateful { get; set; }
		public List<string> SourceGroups { get; set; }
		public List<string> DestinationGroups { get; set; }
	}

	public class Chain
	{
		public string Name { get; set; }
		public int ChainType { get; set; }              // ChainType 枚举的 int 值
		public string Description { get; set; }
		public bool Enabled { get; set; }
		public List<Rule> Rules { get; set; }
		public int DefaultAction { get; set; }           // Action 枚举的 int 值
	}

	public class GroupInfo
	{
		public List<string> Declares { get; set; }
		public List<string> Members { get; set; }
	}

	public class AclV1
	{
		public List<Chain> Chains { get; set; }
		public GroupInfo Group { get; set; }             // 对应 Rust 的 Option
	}
	public class Root
	{
		public Acl Acl { get; set; }
	}

	public class Acl
	{
		[TomlPropertyName("acl_v1")]
		public AclV1 AclV1 { get; set; }                 // 对应 Rust 的 Option
	}

	public static class PaperConnectAclBuilder
	{
		/// <summary>
		/// 辅助方法：创建一个 Allow 类型的规则（所有未显式指定的字段使用默认值）
		/// </summary>
		private static Rule AllowRule(
			string name,
			uint priority,
			Protocol protocol,
			List<string> ports,
			List<string> sourceIPs,
			List<string> destinationIPs,
			List<string> sourcePorts,
			List<int> appProtocols,
			uint rateLimit,
			uint burstLimit,
			List<string> payloadPrefixHex,
			uint? payloadMinLen,
			uint? payloadMaxLen,
			bool? dstIsBroadcast,
			bool? dstIsMulticast)
		{
			return new Rule
			{
				Name = name,
				Description = "",
				Priority = priority,
				Enabled = true,
				Protocol = (int)protocol,
				Ports = new List<string>(ports),                 // 复制，避免外部修改影响
				SourceIPs = new List<string>(sourceIPs),
				DestinationIPs = new List<string>(destinationIPs),
				SourcePorts = new List<string>(sourcePorts),
				AppProtocols = new List<int>(appProtocols),
				PayloadPrefixHex = new List<string>(payloadPrefixHex),
				PayloadMinLen = payloadMinLen,
				PayloadMaxLen = payloadMaxLen,
				DstIsBroadcast = dstIsBroadcast,
				DstIsMulticast = dstIsMulticast,
				Action = (int)Action.Allow,
				RateLimit = rateLimit,
				BurstLimit = burstLimit,
				Stateful = false,
				SourceGroups = new List<string>(),
				DestinationGroups = new List<string>()
			};
		}

		/// <summary>
		/// 构建 PaperConnect/Bedrock 专用的 ACL。
		/// </summary>
		/// <param name="isHost">true 表示当前节点为游戏主机，false 表示加入者</param>
		/// <param name="hostVip">主机的虚拟 IP 地址（例如 "10.144.144.1"）</param>
		/// <param name="hostProtocolPort">主机协议端口（TCP），如果为 null 则允许所有 TCP 端口</param>
		/// <returns>ACL 对象</returns>
		public static Root BuildPaperConnectAcl(bool isHost, string hostVip, ushort? hostProtocolPort)
		{
			var inboundRules = new List<Rule>();
			var outboundRules = new List<Rule>();

			// EasyTier app_protocols: RakNet=10, WebRtc=20, WebRtcStun=21, WebRtcDtls=22, WebRtcRtp=23
			var bedrockUdpAppProtocols = new List<int> { 10, 20, 21, 22, 23 };

			// 广播发现相关常量
			uint discoveryRateLimit = 0;
			uint discoveryBurstLimit = 0;
			uint? discoveryPayloadMinLen = null;
			uint? discoveryPayloadMaxLen = null;
			var discoveryPayloadPrefixHex = new List<string>();

			var discoveryBroadcastPorts = new List<string> { "7551", "19132", "19133" };
			var discoveryBroadcastIPs = new List<string> { "10.144.144.255", "255.255.255.255" };
			var permissiveUnicastPorts = new List<string> { "7551" };

			if (isHost)
			{
				// ---- 主机规则 ----

				// 入站：允许 7551 到主机 VIP（不考虑应用协议）
				inboundRules.Add(AllowRule(
					name: "allow_udp_to_host_unicast_permissive",
					priority: 5200,
					protocol: Protocol.Udp,
					ports: new List<string>(permissiveUnicastPorts),
					sourceIPs: new List<string>(),
					destinationIPs: new List<string> { hostVip },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 入站：允许 LAN 发现广播（客户端发送到 10.144.144.255:7551 等）
				inboundRules.Add(AllowRule(
					name: "allow_udp_discovery_broadcast_in",
					priority: 5000,
					protocol: Protocol.Udp,
					ports: new List<string>(discoveryBroadcastPorts),
					sourceIPs: new List<string>(),
					destinationIPs: new List<string>(discoveryBroadcastIPs),
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: discoveryRateLimit,
					burstLimit: discoveryBurstLimit,
					payloadPrefixHex: new List<string>(discoveryPayloadPrefixHex),
					payloadMinLen: discoveryPayloadMinLen,
					payloadMaxLen: discoveryPayloadMaxLen,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 入站：允许 UDP 到主机 VIP（任意端口，用于发现和游戏流量）
				inboundRules.Add(AllowRule(
					name: "allow_udp_to_host",
					priority: 4500,
					protocol: Protocol.Udp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string>(),
					destinationIPs: new List<string> { hostVip },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(bedrockUdpAppProtocols),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: false,
					dstIsMulticast: null
				));

				// 入站：允许 PaperConnect 控制面 TCP（如果指定了协议端口则限制端口，否则允许全部）
				if (hostProtocolPort.HasValue)
				{
					inboundRules.Add(AllowRule(
						name: "allow_tcp_to_host_protocol_port",
						priority: 4000,
						protocol: Protocol.Tcp,
						ports: new List<string> { hostProtocolPort.Value.ToString() },
						sourceIPs: new List<string>(),
						destinationIPs: new List<string> { hostVip },
						sourcePorts: new List<string>(),
						appProtocols: new List<int>(),
						rateLimit: 0,
						burstLimit: 0,
						payloadPrefixHex: new List<string>(),
						payloadMinLen: null,
						payloadMaxLen: null,
						dstIsBroadcast: null,
						dstIsMulticast: null
					));
				}
				else
				{
					inboundRules.Add(AllowRule(
						name: "allow_tcp_to_host",
						priority: 3500,
						protocol: Protocol.Tcp,
						ports: new List<string> { "0-65535" },
						sourceIPs: new List<string>(),
						destinationIPs: new List<string> { hostVip },
						sourcePorts: new List<string>(),
						appProtocols: new List<int>(),
						rateLimit: 0,
						burstLimit: 0,
						payloadPrefixHex: new List<string>(),
						payloadMinLen: null,
						payloadMaxLen: null,
						dstIsBroadcast: null,
						dstIsMulticast: null
					));
				}

				// 出站：允许 7551 主机 -> 成员单播（不考虑应用协议）
				outboundRules.Add(AllowRule(
					name: "allow_udp_from_host_to_members_unicast_permissive",
					priority: 5200,
					protocol: Protocol.Udp,
					ports: new List<string>(permissiveUnicastPorts),
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 出站：允许主机与成员之间的任意 UDP 端口（RakNet/NetherNet/WebRTC）
				outboundRules.Add(AllowRule(
					name: "allow_udp_from_host_to_members",
					priority: 5000,
					protocol: Protocol.Udp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(bedrockUdpAppProtocols),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: false,
					dstIsMulticast: null
				));

				// 出站：允许主机 TCP 回复/控制流量到成员
				outboundRules.Add(AllowRule(
					name: "allow_tcp_from_host_to_members",
					priority: 4800,
					protocol: Protocol.Tcp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 出站：允许主机广播用于发现
				outboundRules.Add(AllowRule(
					name: "allow_udp_discovery_broadcast_out",
					priority: 4500,
					protocol: Protocol.Udp,
					ports: new List<string>(discoveryBroadcastPorts),
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string>(discoveryBroadcastIPs),
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: discoveryRateLimit,
					burstLimit: discoveryBurstLimit,
					payloadPrefixHex: new List<string>(discoveryPayloadPrefixHex),
					payloadMinLen: discoveryPayloadMinLen,
					payloadMaxLen: discoveryPayloadMaxLen,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));
			}
			else
			{
				// ---- 加入者规则 ----

				// 入站：允许 7551 主机 -> 加入者单播（不考虑应用协议）
				inboundRules.Add(AllowRule(
					name: "allow_udp_from_host_unicast_permissive",
					priority: 5200,
					protocol: Protocol.Udp,
					ports: new List<string>(permissiveUnicastPorts),
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 入站：加入者只接受来自主机 VIP 的入站 UDP（任意端口）
				inboundRules.Add(AllowRule(
					name: "allow_udp_from_host",
					priority: 5000,
					protocol: Protocol.Udp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(bedrockUdpAppProtocols),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: false,
					dstIsMulticast: null
				));

				// 入站：加入者接受来自主机 VIP 的控制面 TCP
				inboundRules.Add(AllowRule(
					name: "allow_tcp_from_host",
					priority: 4500,
					protocol: Protocol.Tcp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string> { hostVip },
					destinationIPs: new List<string> { "10.144.144.0/24" },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 出站：允许 7551 加入者 -> 主机单播（不考虑应用协议）
				outboundRules.Add(AllowRule(
					name: "allow_udp_to_host_unicast_permissive",
					priority: 5200,
					protocol: Protocol.Udp,
					ports: new List<string>(permissiveUnicastPorts),
					sourceIPs: new List<string>(),
					destinationIPs: new List<string> { hostVip },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 出站：加入者只能与主机 VIP 通信（任意 UDP 端口）
				outboundRules.Add(AllowRule(
					name: "allow_udp_to_host",
					priority: 5000,
					protocol: Protocol.Udp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string>(),
					destinationIPs: new List<string> { hostVip },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(bedrockUdpAppProtocols),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: false,
					dstIsMulticast: null
				));

				// 出站：加入者只能与主机 VIP 通信（任意 TCP 端口）
				outboundRules.Add(AllowRule(
					name: "allow_tcp_to_host",
					priority: 4500,
					protocol: Protocol.Tcp,
					ports: new List<string> { "0-65535" },
					sourceIPs: new List<string>(),
					destinationIPs: new List<string> { hostVip },
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: 0,
					burstLimit: 0,
					payloadPrefixHex: new List<string>(),
					payloadMinLen: null,
					payloadMaxLen: null,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));

				// 出站：加入者必须能够广播 7551 用于主机发现
				outboundRules.Add(AllowRule(
					name: "allow_udp_discovery_broadcast_out",
					priority: 4000,
					protocol: Protocol.Udp,
					ports: new List<string>(discoveryBroadcastPorts),
					sourceIPs: new List<string>(),
					destinationIPs: new List<string>(discoveryBroadcastIPs),
					sourcePorts: new List<string>(),
					appProtocols: new List<int>(),
					rateLimit: discoveryRateLimit,
					burstLimit: discoveryBurstLimit,
					payloadPrefixHex: new List<string>(discoveryPayloadPrefixHex),
					payloadMinLen: discoveryPayloadMinLen,
					payloadMaxLen: discoveryPayloadMaxLen,
					dstIsBroadcast: null,
					dstIsMulticast: null
				));
			}

			// 构建入站链
			var inboundChain = new Chain
			{
				Name = "paperconnect_inbound",
				ChainType = (int)ChainType.Inbound,
				Description = "Auto-generated PaperConnect inbound ACL",
				Enabled = true,
				Rules = inboundRules,
				DefaultAction = (int)Action.Drop
			};

			// 构建出站链
			var outboundChain = new Chain
			{
				Name = "paperconnect_outbound",
				ChainType = (int)ChainType.Outbound,
				Description = "Auto-generated PaperConnect outbound ACL",
				Enabled = true,
				Rules = outboundRules,
				DefaultAction = (int)Action.Drop
			};

			// 组装 ACL
			return new Root()
			{
				Acl = new Acl
				{
					AclV1 = new AclV1
					{
						Chains = new List<Chain> { inboundChain, outboundChain },
						Group = new GroupInfo
						{
							Declares = new List<string>(),
							Members = new List<string>()
						}
					}
				}
			};
		}
	}
}
