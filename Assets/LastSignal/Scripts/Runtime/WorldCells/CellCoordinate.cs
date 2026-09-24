using System;
using System.Globalization;
using UnityEngine;

namespace LastSignal.WorldCells
{
    /// <summary>Half-open regions [x*64,(x+1)*64), including negative coordinates.</summary>
    [Serializable]
    public struct CellCoordinate : IEquatable<CellCoordinate>
    {
        public int x, z;
        public const int Size = 64;
        public CellCoordinate(int x, int z) { this.x = x; this.z = z; }
        public string Id => "cell:" + x.ToString(CultureInfo.InvariantCulture) + ":" + z.ToString(CultureInfo.InvariantCulture);
        public Vector3 Minimum => new Vector3((float)((double)x * Size), 0, (float)((double)z * Size));
        public static CellCoordinate FromWorld(Vector3 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.z) ||
                Math.Abs((double)position.x / Size) >= int.MaxValue || Math.Abs((double)position.z / Size) >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(position));
            return new CellCoordinate((int)Math.Floor((double)position.x / Size), (int)Math.Floor((double)position.z / Size));
        }
        public bool Equals(CellCoordinate other) => x == other.x && z == other.z;
        public override bool Equals(object other) => other is CellCoordinate value && Equals(value);
        public override int GetHashCode() => unchecked(x * 397 ^ z);
        public override string ToString() => Id;
    }
    public enum CellState { Unloaded, Requested, Loading, Restoring, Ready, Unloading, Failed }
    public readonly struct CellToken
    {
        public readonly long Session, Operation;
        public CellToken(long session, long operation) { Session = session; Operation = operation; }
    }
    /// <summary>No Unity callbacks can publish readiness without a current token and every prerequisite.</summary>
    public sealed class CellLifecycle
    {
        long session, operation;
        public CellState State { get; private set; }
        public string Failure { get; private set; }
        public CellToken Request(long generation)
        {
            if (State != CellState.Unloaded && State != CellState.Failed) throw new InvalidOperationException("Cell already requested.");
            session = generation; operation = checked(operation + 1); Failure = null; State = CellState.Requested;
            return new CellToken(session, operation);
        }
        public bool Current(CellToken token) => token.Session == session && token.Operation == operation;
        public bool Move(CellToken token, CellState next)
        {
            if (!Current(token)) return false;
            bool valid = (State == CellState.Requested && next == CellState.Loading) ||
                (State == CellState.Loading && next == CellState.Restoring) || (State == CellState.Ready && next == CellState.Unloading) ||
                (State == CellState.Unloading && next == CellState.Unloaded);
            if (!valid) throw new InvalidOperationException($"Invalid cell transition {State} -> {next}");
            State = next; return true;
        }
        public bool Complete(CellToken token, bool content, bool restored, bool entities, bool collision, bool navigation, bool initialized)
        {
            if (!Current(token) || State != CellState.Restoring || !content || !restored || !entities || !collision || !navigation || !initialized) return false;
            State = CellState.Ready; return true;
        }
        public void Fail(CellToken token, string reason)
        { if (Current(token)) { Failure = reason; State = CellState.Failed; } }
        public void Invalidate(long generation)
        { session = generation; operation = checked(operation + 1); State = CellState.Unloaded; Failure = null; }
    }
    [Serializable] public sealed class CellSnapshot
    {
        public string id;
        public bool visited;
        public double lastProcessed;
        public LastSignal.Persistence.WorldSnapshot world;
    }
    [Serializable] public sealed class CellWorldSnapshot
    {
        public string playerCell;
        public CellSnapshot[] cells;
    }
}
