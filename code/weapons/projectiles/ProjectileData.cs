using Sandbox;

namespace Facepunch.Hidden;

[GameResource( "Projectile Data", "proj", "Data that describes a projectile.", Icon = "radio_button_checked" )]
public class ProjectileData : GameResource
{
	[Property]
	public bool FaceDirection { get; set; } = true;

	[Property]
	public RangedFloat LifeTime { get; set; } = 5f;

	[Property]
	public RangedFloat Gravity { get; set; } = 50f;

	[Property]
	public RangedFloat Speed { get; set; } = 2000f;

	[Property]
	public float Radius { get; set; } = 8f;

	[Property, ResourceType( "vmdl" )]
	public string ModelName { get; set; }

	[Property, ResourceType( "vpcf" )]
	public string ExplosionEffect { get; set; }

	[Property, ResourceType( "vpcf" )]
	public string FollowEffect { get; set; }

	[Property, ResourceType( "vpcf" )]
	public string TrailEffect { get; set; }

	[Property, ResourceType( "sound" )]
	public string LaunchSound { get; set; }

	[Property, ResourceType( "sound" )]
	public string HitSound { get; set; }
}
