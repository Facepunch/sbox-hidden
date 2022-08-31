using Sandbox;
using System;

namespace Facepunch.Hidden
{
	partial class Player
	{
		[Net] public int TeamIndex { get; set; }
		public int LastTeamIndex { get; set; }

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
						Client.SetInt("team", TeamIndex);
					}
				}
			}
		}
	}
}
