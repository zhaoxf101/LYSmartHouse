using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LYSmartHouse.Controls
{
    public class MaximumChangedEventArgs : RoutedEventArgs
    {
        public MaximumChangedEventArgs(RoutedEvent routedEvent, bool maximum)
        {
            RoutedEvent = routedEvent;
            Maximum = maximum;
        }


        public bool Maximum { get; set; }
    }

    public delegate void MaximumChangedHandler(object sender, MaximumChangedEventArgs e);

    [TemplatePart(Name = "PART_MaximumToggleButton", Type = typeof(ToggleButton))]
    public class WorkspaceItem : ContentControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(WorkspaceItem));

        // TileMode: 32
        // Collapsed: 40
        // Maximum: 32
        public static readonly DependencyProperty TitleHeightProperty = DependencyProperty.Register("TitleHeight", typeof(double), typeof(WorkspaceItem), new PropertyMetadata(32.0));
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(FrameworkElement), typeof(WorkspaceItem));

        public static readonly DependencyProperty XProperty = Canvas.LeftProperty.AddOwner(typeof(WorkspaceItem), new FrameworkPropertyMetadata(0.0));
        public static readonly DependencyProperty YProperty = Canvas.TopProperty.AddOwner(typeof(WorkspaceItem), new FrameworkPropertyMetadata(0.0));

        public static readonly DependencyProperty TileModeProperty = DependencyProperty.Register("TileMode", typeof(bool), typeof(WorkspaceItem), new PropertyMetadata(true, new PropertyChangedCallback(TileModeChangedCallback)));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(bool), typeof(WorkspaceItem), new PropertyMetadata(false, new PropertyChangedCallback(MaximumChangedCallback)));
        public static readonly DependencyProperty CollapsedProperty = DependencyProperty.Register("Collapsed", typeof(bool), typeof(WorkspaceItem), new PropertyMetadata(true, new PropertyChangedCallback(CollapsedChangedCallback)));


        public static readonly RoutedEvent MaximumChangedEvent = EventManager.RegisterRoutedEvent("MaximumChanged", RoutingStrategy.Bubble, typeof(MaximumChangedHandler), typeof(WorkspaceItem));
        public static readonly RoutedEvent PreviewMaximumChangedEvent = EventManager.RegisterRoutedEvent("PreviewMaximumChanged", RoutingStrategy.Tunnel, typeof(MaximumChangedHandler), typeof(WorkspaceItem));

        public static readonly RoutedEvent ContentDisplayedEvent = EventManager.RegisterRoutedEvent("ContentDisplayed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WorkspaceItem));
        public static readonly RoutedEvent PreviewContentDisplayedEvent = EventManager.RegisterRoutedEvent("PreviewContentDisplayed", RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(WorkspaceItem));

        static WorkspaceItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WorkspaceItem), new FrameworkPropertyMetadata(typeof(WorkspaceItem)));


            //PropertyMetadata md = Canvas.LeftProperty.GetMetadata(typeof(WorkspaceItem));
        }

        static void TileModeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("WorkspaceItem. TileModeChangedCallback");
        }

        static void MaximumChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WorkspaceItem wi = d as WorkspaceItem;

            if (wi != null)
            {
                wi._maximumToggleButton.IsChecked = (bool)e.NewValue;
            }
        }

        static void CollapsedChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        bool _raiseMaximumChangedEvent = true;
        ToggleButton _maximumToggleButton;
        bool _mouseNotLeaveNotified = false;

        public WorkspaceItem()
        {

            //MouseEventHandler
            Mouse.AddMouseEnterHandler(this, (sender, e) =>
                {
                    //Debug.WriteLine("WorkspaceItem::MouseEnterHandler. e.OriginalSource: " + e.OriginalSource + " Mouse.Captured: " + Mouse.Captured);
                    //Debug.WriteLine("  this.IsMouseOver: " + this.IsMouseOver);
                    //Debug.WriteLine("  Mouse.DirectlyOver: " + Mouse.DirectlyOver);

                    MaximumToggleButton.Visibility = System.Windows.Visibility.Visible;
                });

            Mouse.AddMouseLeaveHandler(this, (sender, e) =>
                {
                    //Debug.WriteLine("WorkspaceItem::MouseLeaveHandler. e.OriginalSource: " + e.OriginalSource + " Mouse.Captured: " + Mouse.Captured);

                    //Debug.WriteLine("Mouse.DirectlyOver: " + Mouse.DirectlyOver);
                    //Debug.WriteLine("  this.IsMouseOver: " + this.IsMouseOver);

                    //Debug.WriteLine("  Mouse.DirectlyOver: " + Mouse.DirectlyOver);

                    if (!_mouseNotLeaveNotified)
                    {
                        MaximumToggleButton.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        _mouseNotLeaveNotified = false;
                    }
                });

            //Mouse.AddMouseDownHandler(this, (sender, e) =>
            //    {
            //        Debug.WriteLine("WorkspaceItem::MouseDownHandler. e.OriginalSource: " + e.OriginalSource + " Mouse.Captured: " + Mouse.Captured);
            //    });


            //Loaded += WorkspaceItem_Loaded;

            //Debug.WriteLine("WorkspaceItem(). X: " + X + " Y: " + Y);
        }

        public double X
        {
            get { return (double)GetValue(Canvas.LeftProperty); }
            set { SetValue(Canvas.LeftProperty, value); }
        }

        public double Y
        {
            get { return (double)GetValue(Canvas.TopProperty); }
            set { SetValue(Canvas.TopProperty, value); }
        }


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public double TitleHeight
        {
            get { return (double)GetValue(TitleHeightProperty); }
            set { SetValue(TitleHeightProperty, value); }
        }

        public FrameworkElement Icon
        {
            get { return (FrameworkElement)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public bool Maximum
        {
            get { return (bool)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public bool Collapsed
        {
            get { return (bool)GetValue(CollapsedProperty); }
            set { SetValue(CollapsedProperty, value); }
        }


        ToggleButton MaximumToggleButton
        {
            get { return _maximumToggleButton; }
            set
            {
                // ToggleButton MouseDown 可能有子元素处理
                // MouseDoubleClick 可能没有子元素处理
                if (_maximumToggleButton != null)
                {
                    //_maximumToggleButton.PreviewMouseDown -= _maximumToggleButton_PreviewMouseDown;
                    //_maximumToggleButton.PreviewMouseDoubleClick -= _maximumToggleButton_PreviewMouseDoubleClick;

                    //_maximumToggleButton.MouseEnter -= _maximumToggleButton_MouseEnter;
                    //_maximumToggleButton.MouseLeave -= _maximumToggleButton_MouseLeave;

                    _maximumToggleButton.Checked -= _maximumToggleButton_Checked;
                    _maximumToggleButton.Unchecked -= _maximumToggleButton_Unchecked;
                }

                _maximumToggleButton = value;

                if (_maximumToggleButton != null)
                {
                    //_maximumToggleButton.PreviewMouseDown += _maximumToggleButton_PreviewMouseDown;
                    //_maximumToggleButton.PreviewMouseDoubleClick += _maximumToggleButton_PreviewMouseDoubleClick;

                    //_maximumToggleButton.MouseEnter += _maximumToggleButton_MouseEnter;
                    //_maximumToggleButton.MouseLeave += _maximumToggleButton_MouseLeave;

                    _maximumToggleButton.Checked += _maximumToggleButton_Checked;
                    _maximumToggleButton.Unchecked += _maximumToggleButton_Unchecked;
                }

            }
        }

        public override void OnApplyTemplate()
        {
            MaximumToggleButton = GetTemplateChild("PART_MaximumToggleButton") as ToggleButton;

            Debug.WriteLine("WorkspaceItem.OnApplyTemplate. TileModeToggleButton: " + MaximumToggleButton);
        }

        public void SetMaximum(bool maximum, bool raiseEvent)
        {
            if (Maximum != maximum)
            {
                _raiseMaximumChangedEvent = raiseEvent;

                Maximum = maximum;
            }
        }

        public void NotifyMouseNotLeave()
        {
            _mouseNotLeaveNotified = true;
        }

        //void _maximumToggleButton_MouseEnter(object sender, MouseEventArgs e)
        //{
        //    Debug.WriteLine("_maximumToggleButton_MouseEnter. Mouse.Captured: " + Mouse.Captured);
        //}

        //void _maximumToggleButton_MouseLeave(object sender, MouseEventArgs e)
        //{

        //    Debug.WriteLine("_maximumToggleButton_MouseLeave. Mouse.Captured: " + Mouse.Captured);
        //}

        //void _maximumToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    //e.Handled = true;
        //    Debug.WriteLine("TileModeToggleButton_MouseDown. e.Handled: " + e.Handled);
        //}

        //private void _maximumToggleButton_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    Debug.WriteLine("TileModeToggleButton_PreviewMouseDoubleClick. e.Handled:" + e.Handled);
        //    //e.Handled = true;
        //}

        private void _maximumToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TileModeToggleButton Checked. IsChecked: " + _maximumToggleButton.IsChecked);

            //TitleHeightK

            Maximum = true;

            if (_raiseMaximumChangedEvent)
            {
                RaiseEvent(new MaximumChangedEventArgs(PreviewMaximumChangedEvent, true));
                RaiseEvent(new MaximumChangedEventArgs(MaximumChangedEvent, true));
            }
            else
            {
                _raiseMaximumChangedEvent = true;
            }
        }

        private void _maximumToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("TileModeToggleButton Unchecked. IsChecked: " + _maximumToggleButton.IsChecked);

            //TitleHeight = 32;
            Maximum = false;

            if (_raiseMaximumChangedEvent)
            {
                RaiseEvent(new MaximumChangedEventArgs(PreviewMaximumChangedEvent, false));
                RaiseEvent(new MaximumChangedEventArgs(MaximumChangedEvent, false));
            }
            else
            {
                _raiseMaximumChangedEvent = true;
            }
        }

    }
}
