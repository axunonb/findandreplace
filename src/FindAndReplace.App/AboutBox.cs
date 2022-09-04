﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FindAndReplace.App
{
	partial class AboutBox : Form
	{
		public AboutBox()
		{
			InitializeComponent();
			this.Text = string.Format("About {0}", AssemblyTitle);
			this.lblProductName.Text = AssemblyProduct;
			this.lnkProduct.Text = "http://findandreplace.io/";
		
			this.lblVersion.Text = string.Format("Version {0}", AssemblyVersion);
			this.lblCopyright.Text = AssemblyCopyright;
			this.lnkCompany.Text = AssemblyCompany;
		    this.uiSupportedBy.Text = "ZZZ Projects";
        }

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (attributes.Length > 0)
				{
					var titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if (titleAttribute.Title != "")
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
			}
		}

		public string AssemblyDescription
		{
			get
			{
				var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyDescriptionAttribute)attributes[0]).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyProductAttribute)attributes[0]).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
				if (attributes.Length == 0)
				{
					return "";
				}
				return ((AssemblyCompanyAttribute)attributes[0]).Company;
			}
		}
		#endregion

		private void lnkProduct_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Tools.LaunchBrowser("http://findandreplace.io/");
		}

		private void lnkCompany_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
            Tools.LaunchBrowser("http://www.entechsolutions.com");
		}

        private void uiSupportedBy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Tools.LaunchBrowser("http://www.zzzprojects.com/");
        }
    }
}
