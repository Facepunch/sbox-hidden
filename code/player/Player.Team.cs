using Sandbox;
using System;

namespace HiddenGamemode
{
	partial class Player
	{
		BaseTeam _team;

		public BaseTeam Team
		{
			get => _team;

			set
			{
				// A player must be on a valid team.
				if ( value != null )
				{
					_team?.Leave( this );
					_team = value;
					_team.Join( this );

					if (IsServer)
						ChangeTeam( _team.NetworkIdent );
				}
			}
		}

		[ClientRpc]
		private void ChangeTeam( int entityId )
		{
			Team = FindByIndex( entityId ) as BaseTeam;
			Assert.NotNull( _team );
		}
	}
}
