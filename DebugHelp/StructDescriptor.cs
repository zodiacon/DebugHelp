using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zodiacon.DebugHelp {

	[DebuggerDisplay("{Name,nq} offset={Offset,d} size={Size,d}")]
	public sealed class StructMember {
		public readonly int Offset;
		public StructDescriptor Parent { get; internal set; }
		public readonly SymbolInfo Symbol;
        public string Name => Symbol.Name;
        public int Size => Symbol.Size;
        public long Value;

		public StructMember(in SymbolInfo symbol, int offset) {
			Symbol = symbol;
			Offset = offset;
		}

		public StructMember Clone() {
			var member = (StructMember)MemberwiseClone();
			member.Parent = null;
			return member;
		}

		public int TypeId => Symbol.TypeIndex;

		public override string ToString() => $"{Symbol.Name}, size={Symbol.Size}, offset={Offset}, typeid={Symbol.TypeIndex} tag={Symbol.Tag}";
	}

	public sealed class StructDescriptor : IReadOnlyList<StructMember> {

		readonly Dictionary<string, StructMember> _membersByName;
		readonly List<StructMember> _members;

		internal StructDescriptor(int capacity = 8) {
			_membersByName = new Dictionary<string, StructMember>(capacity, StringComparer.InvariantCultureIgnoreCase);
			_members = new List<StructMember>(capacity);
		}

		public int GetOffsetOf(string memberName) {
			return _membersByName.TryGetValue(memberName, out var member) ? member.Offset : -1;
		}

		public StructMember GetMember(string memberName) {
			return _membersByName.TryGetValue(memberName, out var member) ? member : null;
		}

		public int Length { get; internal set; }

		public int Count => _members.Count;

		public StructMember this[int index] => _members[index];

		public void AddMember(StructMember member) {
			member.Parent = this;
			_membersByName.Add(member.Symbol.Name, member);
			_members.Add(member);
		}

		public IEnumerator<StructMember> GetEnumerator() {
			return _members.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
