﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiddenGamemode
{
	public class HideRound : BaseRound
	{
		public override int RoundDuration => 20;

		protected override void OnStart()
		{
			Log.Info( "Started Hide Round" );

			Sandbox.Player.All.ForEach((player) => player.Respawn());
		}

		protected override void OnFinish()
		{
			Log.Info( "Finished Hide Round" );
		}

		protected override void OnTimeUp()
		{
			Log.Info( "Hide Time Up!" );

			Game.CurrentRound = new HuntRound();

			base.OnTimeUp();
		}

		public override void OnPlayerSpawn( Player player )
		{
			if ( Players.Contains( player ) ) return;

			player.SetModel( "models/citizen/citizen.vmdl" );

			player.Controller = new WalkController();
			player.Camera = new FirstPersonCamera();

			player.EnableAllCollisions = true;
			player.EnableDrawing = true;
			player.EnableHideInFirstPerson = true;
			player.EnableShadowInFirstPerson = true;

			if ( Rand.Int( 1, 2 ) == 1 )
			{
				player.Team = Game.HiddenTeam;
			}
			else
			{
				player.Team = Game.MilitaryTeam;
			}

			player.ClearAmmo();
			player.Inventory.DeleteContents();

			AddPlayer( player );

			base.OnPlayerSpawn( player );
		}
	}
}
