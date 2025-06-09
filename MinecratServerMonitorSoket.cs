using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class MinecraftServerMonitorSoket
{
    public static async Task<ServerStatus?> GetMinecraftStatusAsync(string host, int port)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port);
        using var stream = client.GetStream();

        var writer = new BinaryWriter(stream);
        var reader = new BinaryReader(stream);

        // === Packet 0: Handshake ===
        using var ms = new MemoryStream();
        var handshake = new BinaryWriter(ms);
        handshake.Write((byte)0x00); // Packet ID
        WriteVarInt(handshake, 760); // Protocol version (1.20.4)
        WriteVarInt(handshake, host.Length);
        handshake.Write(Encoding.UTF8.GetBytes(host)); // Host
        handshake.Write((ushort)port); // Port
        WriteVarInt(handshake, 1); // Next state: status

        WritePacket(writer, ms.ToArray());

        // === Packet 1: Status Request ===
        WritePacket(writer, new byte[] { 0x00 });

        // === Read response ===
        int _ = ReadVarInt(reader); // Packet length
        int packetId = ReadVarInt(reader); // Packet ID
        if (packetId != 0x00) return null;

        int jsonLength = ReadVarInt(reader);
        string json = Encoding.UTF8.GetString(reader.ReadBytes(jsonLength));

        var status = JsonSerializer.Deserialize<ServerStatus>(json);
        return status;
    }

    static void WritePacket(BinaryWriter writer, byte[] data)
    {
        using var ms = new MemoryStream();
        WriteVarInt(ms, data.Length);
        ms.Write(data, 0, data.Length);
        writer.Write(ms.ToArray());
    }

    static void WriteVarInt(Stream stream, int value)
    {
        while ((value & -128) != 0)
        {
            stream.WriteByte((byte)((value & 127) | 128));
            value >>= 7;
        }
        stream.WriteByte((byte)value);
    }

    static void WriteVarInt(BinaryWriter writer, int value)
    {
        while ((value & -128) != 0)
        {
            writer.Write((byte)((value & 127) | 128));
            value >>= 7;
        }
        writer.Write((byte)value);
    }

    static int ReadVarInt(BinaryReader reader)
    {
        int numRead = 0, result = 0;
        byte read;
        do
        {
            read = reader.ReadByte();
            int value = (read & 0b01111111);
            result |= (value << (7 * numRead));
            numRead++;

            if (numRead > 5) throw new Exception("VarInt too big");
        } while ((read & 0b10000000) != 0);

        return result;
    }

    // === Models ===
    public class ServerStatus
    {
        public Version version { get; set; }
        public Players players { get; set; }
        public string description { get; set; }
    }

    public class Version
    {
        public string name { get; set; }
        public int protocol { get; set; }
    }

    public class Players
    {
        public int max { get; set; }
        public int online { get; set; }
        public Player[] sample { get; set; }
    }

    public class Player
    {
        public string name { get; set; }
        public string id { get; set; }
    }
}
