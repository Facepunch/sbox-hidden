using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;


namespace Facepunch.Hidden
{
	public partial class ChatEntry : Panel
	{
		public Label NameLabel { get; internal set; }
		public Label Message { get; internal set; }
		public Image Avatar { get; internal set; }

		public RealTimeSince TimeSinceBorn = 0;

		public ChatEntry()
		{
			Avatar = Add.Image();
			NameLabel = Add.Label( "Name", "name" );
			Message = Add.Label( "Message", "message" );
		}

		public override void Tick() 
		{
			base.Tick();

			SetClass( "faded", TimeSinceBorn > 10f );
		}
	}
}
