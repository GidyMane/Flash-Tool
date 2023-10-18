using System;
using System.Deployment.Application;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace M32RR_FLASH_TOOL { 

public partial class WindowAbout : Window, IComponentConnector
{
	public WindowAbout()
	{
		InitializeComponent();
		textBlock_version.Text = getRunningVersion().ToString();
	}

	private Version getRunningVersion()
	{
		try
		{
			return ApplicationDeployment.CurrentDeployment.CurrentVersion;
		}
		catch (Exception)
		{
			return Assembly.GetExecutingAssembly().GetName().Version;
		}
	}
}
}
