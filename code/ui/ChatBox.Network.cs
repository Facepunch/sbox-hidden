using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Facepunch.Hidden;

public partial class ChatBox
{
	[ClientRpc]
	public static void AddChatFromServer( HiddenPlayer player, string message, Color color, Color messageColor )
	{
		if ( player.IsValid() )
		{
			Current?.AddEntry( player.Client.Name, message, color, messageColor );

			if ( !Global.IsListenServer )
			{
				Log.Info( $"{player.Client.Name}: {message}" );
			}
		}
	}

	[ConCmd.Client( "hdn_chat_add", CanBeCalledFromServer = true )]
	public static void AddChatEntry( string playerId, string message )
	{
		var client = Client.All.FirstOrDefault( c => c.SteamId == long.Parse( playerId ) );

		if ( client.IsValid() && client.Pawn is HiddenPlayer player && player.Team is not null )
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
		AddChatEntry( To.Everyone, ConsoleSystem.Caller.SteamId.ToString(), message );
	}
}
