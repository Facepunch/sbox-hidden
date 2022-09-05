using Sandbox;
using System;

namespace Facepunch.Hidden
{
	partial class Player
	{
		[Net, Change( nameof( OnTeamIndexChanged ) )] public int TeamIndex { get; set; }

		private BaseTeam CurrentTeam;

		public BaseTeam Team
		{
			get => CurrentTeam;

			set
			{
				// A player must be on a valid team.
				if ( value != null && value != CurrentTeam )
				{
					CurrentTeam?.Leave( this );
					CurrentTeam = value;
					CurrentTeam.Join( this );

					if ( IsServer )
					{
						TeamIndex = CurrentTeam.Index;
						Client.SetInt( "team", TeamIndex );
					}
				}
			}
		}

		private void OnTeamIndexChanged( int teamIndex )
		{
			Team = Game.Instance.GetTeamByIndex( teamIndex );
		}
	}
}
