using Sandbox;
using System.Linq;

namespace Facepunch.Hidden
{
	public interface ICamera
	{
		public void Activated();
		public void Deactivated();
		public void Update();
	}
}
