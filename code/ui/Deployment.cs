
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
			Title = Add.Label( "Select Deployment", "title" );
		}

		public void AddDeployment( DeploymentInfo info )
		{
			var panel = Container.Add.Panel( "deploymentPanel" );

			panel.Add.Label( info.Title, "deploymentTitle" );
			panel.Add.Label( info.Description, "deploymentDesc" );
			panel.Add.Button( "Deploy", "deploymentButton", () => info.OnDeploy() );

			panel.AddClass( info.ClassName );
		}
	}
}
