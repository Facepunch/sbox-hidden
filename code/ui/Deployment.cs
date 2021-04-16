
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace HiddenGamemode
{
	public struct DeploymentInfo
	{
		public string Title;
		public string Description;
		public string ClassName;
		public Action OnDeploy;
	}

	public class Deployment : Panel
	{
		public Panel Container;
		public Label Title;

		public Deployment()
		{
			StyleSheet.Load( "/ui/deployment.scss" );
			Container = Add.Panel( "deploymentContainer" );
			Title = Add.Label( "SELECT DEPLOYMENT", "title" );
		}

		public void AddDeployment( DeploymentInfo info )
		{
			var panel = Container.Add.Panel( "item" );

			panel.Add.Label( info.Title, "title" );
			panel.Add.Label( info.Description, "desc" );
			panel.Add.Button( "DEPLOY", "button", () => info.OnDeploy() );

			panel.AddClass( info.ClassName );
		}
	}
}
