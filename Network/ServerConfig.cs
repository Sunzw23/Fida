using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Fida.Network
{
    /// <summary>
    /// 服务器配置条目
    /// </summary>
    public class ServerEntry
    {
        public string Address { get; set; } = "::1";
        public int Port { get; set; } = 8888;
        public string Name { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Name} ([{Address}]:{Port})";
        }
    }

    /// <summary>
    /// 服务器配置管理器，从 CSV 文件读取服务器地址列表
    /// CSV 格式: Address,Port,Name
    /// 支持 IPv6 地址
    /// 以 # 开头的行为注释
    /// </summary>
    public class ServerConfig
    {
        private readonly List<ServerEntry> _servers = new();

        /// <summary>
        /// 获取第一个服务器的地址
        /// </summary>
        public string GetServerAddress()
        {
            if (_servers.Count > 0)
                return _servers[0].Address;
            return "::1";
        }

        /// <summary>
        /// 获取第一个服务器的端口
        /// </summary>
        public int GetServerPort()
        {
            if (_servers.Count > 0)
                return _servers[0].Port;
            return 8888;
        }

        /// <summary>
        /// 获取所有服务器条目
        /// </summary>
        public List<ServerEntry> GetServers()
        {
            return _servers;
        }

        /// <summary>
        /// 从 CSV 文件加载服务器配置
        /// </summary>
        /// <param name="filePath">CSV 文件路径</param>
        /// <returns>ServerConfig 实例</returns>
        public static ServerConfig Load(string filePath)
        {
            var config = new ServerConfig();

            if (!File.Exists(filePath))
            {
                // 如果文件不存在，返回默认的本地测试服务器
                config._servers.Add(new ServerEntry
                {
                    Address = "::1",
                    Port = 8888,
                    Name = "本地测试服务器 (默认)"
                });
                return config;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                // 跳过空行和注释行
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                string[] parts = trimmed.Split(',');
                if (parts.Length < 2)
                    continue;

                string addressStr = parts[0].Trim();
                string portStr = parts[1].Trim();
                string name = parts.Length >= 3 ? parts[2].Trim() : addressStr;

                // 去掉 IPv6 地址的方括号
                addressStr = addressStr.Trim('[', ']');

                if (!int.TryParse(portStr, out int port) || port <= 0 || port > 65535)
                    continue;

                config._servers.Add(new ServerEntry
                {
                    Address = addressStr,
                    Port = port,
                    Name = name
                });
            }

            // 如果没有加载到任何服务器，添加默认本地服务器
            if (config._servers.Count == 0)
            {
                config._servers.Add(new ServerEntry
                {
                    Address = "::1",
                    Port = 8888,
                    Name = "本地测试服务器 (默认)"
                });
            }

            return config;
        }

        /// <summary>
        /// 创建默认的 CSV 配置文件
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        public static void CreateDefaultConfig(string filePath)
        {
            string content = @"# Fida 联机服务器配置文件
# 格式: Address,Port,Name
# 支持 IPv6 地址
# 以 # 开头的行为注释
#
# 示例:
::1,8888,本地测试服务器
# 2001:db8::1,8888,远程服务器1
# fe80::1,9999,局域网服务器
";

            File.WriteAllText(filePath, content);
        }
    }
}