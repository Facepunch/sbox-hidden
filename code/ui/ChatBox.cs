using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Linq;
using Sandbox;

namespace Facepunch.Hidden
{
	public partial class ChatBox : Panel
	{
		private static ChatBox Current { get; set; }

		public TextEntry Input { get; protected set; }
		public Panel Canvas { get; protected set; }

		[ConCmd.Client( "hdn_chat_add", CanBeCalledFromServer = true )]
		public static void AddChatEntry( string playerId, string message )
		{
			var client = Client.All.FirstOrDefault( c => c.PlayerId == long.Parse( playerId ) );

			if ( client.IsValid() && client.Pawn is Player player )
			{
				Current?.AddEntry( client.Name, message, player.Team.Color );

				if ( !Global.IsListenServer )
				{
					Log.Info( $"{client.Name}: {message}" );
				}
			}
		}

		[ConCmd.Client( "hdn_chat_info", CanBeCalledFromServer = true )]
		public static void AddInformation( string message )
		{
			Current?.AddEntry( null, message, Color.Parse( "#c2b576" ).Value );
		}

		[ConCmd.Server( "hdn_say" )]
		public static void Say( string message )
		{
			Assert.NotNull( ConsoleSystem.Caller );

			if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
				return;

			Log.Info( $"{ConsoleSystem.Caller}: {message}" );
			AddChatEntry( To.Everyone, ConsoleSystem.Caller.PlayerId.ToString(), message );
		}

		public ChatBox()
		{
			Current = this;

			StyleSheet.Load( "/ui/ChatBox.scss" );

			Canvas = Add.Panel( "chat_canvas" );

			Input = Add.TextEntry( "" );
			Input.AddEventListener( "onsubmit", () => Submit() );
			Input.AddEventListener( "onblur", () => Close() );
			Input.AcceptsFocus = true;
			Input.AllowEmojiReplace = true;
		}

		public override void Tick()
		{
			base.Tick();

			if ( Sandbox.Input.Pressed( InputButton.Chat ) )
			{
				Open();
			}
		}

		public void AddEntry( string name, string message, Color? color = null )
		{
			var e = Canvas.AddChild<ChatEntry>();

			e.Message.Text = message;
			e.NameLabel.Text = name;

			e.SetClass( "noname", string.IsNullOrEmpty( name ) );

			if ( color.HasValue )
			{
				e.NameLabel.Style.FontColor = color;
			}

			if ( string.IsNullOrEmpty( name ) )
			{
				e.Message.Style.FontColor = color;
			}
		}

		private bool CanTalkInChat()
		{
			if ( !Local.Pawn.IsValid() || Local.Pawn.LifeState == LifeState.Dead )
				return false;

			return true;
		}

		private void Open()
		{
			if ( !CanTalkInChat() )
				return;

			AddClass( "open" );
			Input.Focus();
		}

		private void Close()
		{
			RemoveClass( "open" );
			Input.Blur();
		}

		private void Submit()
		{
			Close();

			var msg = Input.Text.Trim();
			Input.Text = "";

			if ( !CanTalkInChat() )
				return;

			if ( string.IsNullOrWhiteSpace( msg ) )
				return;

			Say( msg );
		}
	}
}
