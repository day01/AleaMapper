using System.Linq.Expressions;
using System.Reflection;

namespace Tex.Builders.Map
{
	public class MapMember
	{
		public MapMember(MemberInfo memSource, MemberInfo memDestination)
		{
			MemberSource = memSource;
			MemberDestination = memDestination;
			StateOfMapMember = StateOfMapMember.Default;
		}
		public MapMember(MemberInfo memDestination)
		{
			MemberDestination = memDestination;
			StateOfMapMember = StateOfMapMember.NotImplemented;
		}
		public StateOfMapMember StateOfMapMember { get; set; }
		public MemberInfo MemberSource { get; set; }
		public MemberInfo MemberDestination { get; set; }
		public Expression MapExpression { get; set; }
	}
}
