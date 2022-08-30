using Sandbox;

namespace Facepunch.Hidden
{
	public interface IHudRenderer : IValid
	{
		public void RenderHud( Vector2 screenSize );
	}
}
