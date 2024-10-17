using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AppUI.Classes
{
    /// <summary>
    /// Button that has a UriSource property to include an icon
    /// </summary>
    public class ButtonWithImage : Button
    {
        static ButtonWithImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ButtonWithImage), new FrameworkPropertyMetadata(typeof(ButtonWithImage)));
        }

        #region Public properties

        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        public ControlTemplate TemplateSource
        {
            get { return (ControlTemplate)GetValue(TemplateSourceProperty); }
            set { SetValue(TemplateSourceProperty, value); }
        }


        public SolidColorBrush ImageForeground
        {
            get { return (SolidColorBrush)GetValue(ImageForegroundProperty); }
            set { SetValue(ImageForegroundProperty, value); }
        }

        public double ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public double ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }


        #endregion


        #region Dependency Properties


        public static readonly DependencyProperty UriSourceProperty =
            DependencyProperty.Register(
            "UriSource",
            typeof(Uri),
            typeof(ButtonWithImage),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty TemplateSourceProperty =
            DependencyProperty.Register(
            "TemplateSource",
            typeof(ControlTemplate),
            typeof(ButtonWithImage),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ImageForegroundProperty =
            DependencyProperty.Register(
            "ImageForeground",
            typeof(SolidColorBrush),
            typeof(ButtonWithImage),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register(
            "ImageHeight",
            typeof(double),
            typeof(ButtonWithImage),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register(
            "ImageWidth",
            typeof(double),
            typeof(ButtonWithImage),
            new FrameworkPropertyMetadata(null));

        #endregion
    }
}
