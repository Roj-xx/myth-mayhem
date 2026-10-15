using System.IO;

namespace MythMayhem.Gameplay
{
    // Per-lane replicated state for one creature. CardName identifies the card so
    // both peers resolve it through their own (identical) definition set.
    public struct CreatureNetState
    {
        public bool Present;
        public string CardName;
        public int Health;
        public int AttackMod;
        public int ModTurns;
    }

    public struct BuildingNetState
    {
        public bool Present;
        public string CardName;
        public int Health;
    }

    // The complete authoritative board. Turn, points and hero health live here too,
    // so a single snapshot fully determines what every client should render.
    public struct BoardSnapshot
    {
        public const int SideCount = 2;

        public int Revision;
        public int ActiveSide;    // (int)CreatureSide
        public int PlayerPoints;
        public int OpponentPoints;
        public int PlayerHeroHp;
        public int OpponentHeroHp;
        public int Outcome;       // (int)MatchOutcome

        // Index = (int)side * LaneCount + lane. LaneCount is fixed at 3 by the board.
        public CreatureNetState[] Creatures;
        public BuildingNetState[] Buildings;

        public static BoardSnapshot CreateEmpty(int laneCount)
        {
            return new BoardSnapshot
            {
                Creatures = new CreatureNetState[SideCount * laneCount],
                Buildings = new BuildingNetState[SideCount * laneCount],
            };
        }

        public static int Index(CreatureSide side, int lane, int laneCount)
        {
            return (int)side * laneCount + lane;
        }
    }

    // Manual little-endian packing. We send the snapshot as a byte[] over an RPC,
    // which avoids tying this gameplay type to any netcode serializer.
    public static class BoardSnapshotCodec
    {
        public static byte[] Encode(BoardSnapshot snapshot, int laneCount)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(snapshot.Revision);
                writer.Write(snapshot.ActiveSide);
                writer.Write(snapshot.PlayerPoints);
                writer.Write(snapshot.OpponentPoints);
                writer.Write(snapshot.PlayerHeroHp);
                writer.Write(snapshot.OpponentHeroHp);
                writer.Write(snapshot.Outcome);
                writer.Write(laneCount);

                int count = snapshot.Creatures != null ? snapshot.Creatures.Length : 0;
                writer.Write(count);
                for (int i = 0; i < count; i++)
                {
                    CreatureNetState c = snapshot.Creatures[i];
                    writer.Write(c.Present);
                    writer.Write(c.CardName ?? string.Empty);
                    writer.Write(c.Health);
                    writer.Write(c.AttackMod);
                    writer.Write(c.ModTurns);
                }

                int buildingCount = snapshot.Buildings != null ? snapshot.Buildings.Length : 0;
                writer.Write(buildingCount);
                for (int i = 0; i < buildingCount; i++)
                {
                    BuildingNetState b = snapshot.Buildings[i];
                    writer.Write(b.Present);
                    writer.Write(b.CardName ?? string.Empty);
                    writer.Write(b.Health);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        public static BoardSnapshot Decode(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                BoardSnapshot snapshot = new BoardSnapshot
                {
                    Revision = reader.ReadInt32(),
                    ActiveSide = reader.ReadInt32(),
                    PlayerPoints = reader.ReadInt32(),
                    OpponentPoints = reader.ReadInt32(),
                    PlayerHeroHp = reader.ReadInt32(),
                    OpponentHeroHp = reader.ReadInt32(),
                    Outcome = reader.ReadInt32(),
                };

                int laneCount = reader.ReadInt32();
                int creatureCount = reader.ReadInt32();
                snapshot.Creatures = new CreatureNetState[creatureCount];
                for (int i = 0; i < creatureCount; i++)
                {
                    snapshot.Creatures[i] = new CreatureNetState
                    {
                        Present = reader.ReadBoolean(),
                        CardName = reader.ReadString(),
                        Health = reader.ReadInt32(),
                        AttackMod = reader.ReadInt32(),
                        ModTurns = reader.ReadInt32(),
                    };
                }

                int buildingCount = reader.ReadInt32();
                snapshot.Buildings = new BuildingNetState[buildingCount];
                for (int i = 0; i < buildingCount; i++)
                {
                    snapshot.Buildings[i] = new BuildingNetState
                    {
                        Present = reader.ReadBoolean(),
                        CardName = reader.ReadString(),
                        Health = reader.ReadInt32(),
                    };
                }

                return snapshot;
            }
        }
    }
}
