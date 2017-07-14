using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LYSmartHouse.Controls
{
    [TemplatePart(Name = "PART_Canvas", Type = typeof(Canvas))]
    public class WorkspaceItemsControl : ItemsControl
    {
        struct ItemInfo
        {
            public Point position;
            public Size size;
            public WorkspaceItem item;
        }

        class AnimationHelper
        {
            struct ActiveAnimationInfo
            {
                public UIElement element;
                public DependencyProperty dp;
                public DoubleAnimation animation;
            }

            Dictionary<AnimationClock, ActiveAnimationInfo> _activeAnimations = new Dictionary<AnimationClock, ActiveAnimationInfo>();


            void AnimationCompleted(object sender, EventArgs e)
            {
                //Debug.WriteLine("AnimationCompleted.");

                AnimationClock clock = (AnimationClock)sender;
                ActiveAnimationInfo info = _activeAnimations[clock];

                object value = info.element.GetValue(info.dp);
                info.element.BeginAnimation(info.dp, null);
                info.element.SetValue(info.dp, value);
                info.element.ApplyAnimationClock(info.dp, null);

                _activeAnimations.Remove(clock);

                //Debug.WriteLine("  _activeAnimations.Count: " + _activeAnimations.Count);
            }

            public bool HasActiveClock()
            {
                return _activeAnimations.Count > 0;
            }


            public void BeginAnimation(UIElement element, DependencyProperty dp, double to)
            {
                DoubleAnimation animation = new DoubleAnimation();

                animation.To = to;
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.4));
                animation.DecelerationRatio = 0.5;
                animation.Completed += AnimationCompleted;

                AnimationClock clock = animation.CreateClock();

                element.ApplyAnimationClock(dp, clock);

                _activeAnimations.Add(clock, new ActiveAnimationInfo { element = element, dp = dp, animation = animation });

                //Debug.WriteLine("  _activeAnimations.Count: " + _activeAnimations.Count);
            }

            public void RemoveAll()
            {
                if (_activeAnimations.Count == 0)
                {
                    return;
                }

                foreach (var item in _activeAnimations.Keys)
                {
                    ActiveAnimationInfo info = _activeAnimations[item];

                    object value = info.element.GetValue(info.dp);
                    info.element.BeginAnimation(info.dp, null);
                    info.element.SetValue(info.dp, value);
                    info.element.ApplyAnimationClock(info.dp, null);

                    //item.Controller.Remove();
                    item.Controller.Stop();
                }

                _activeAnimations.Clear();
            }

        }

        //表示当前是否处于平铺状态。
        public static readonly DependencyProperty TileModeProperty = DependencyProperty.Register("TileMode", typeof(bool), typeof(WorkspaceItemsControl), new PropertyMetadata(true, new PropertyChangedCallback(TileModeChangedCallback)));

        static WorkspaceItemsControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WorkspaceItemsControl), new FrameworkPropertyMetadata(typeof(WorkspaceItemsControl)));


        }

        private static void TileModeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //WorkspaceItemsControl itemsControl = d as WorkspaceItemsControl;

            WorkspaceItem wi = d as WorkspaceItem;

            if (wi != null)
            {
                Debug.WriteLine("TileModeChanged. Item: " + wi.Title + " NewValue: " + e.NewValue);

            }
            else
            {
                Debug.WriteLine("TileModeChanged. ItemType: " + d + " NewValue: " + e.NewValue);
            }
        }

        // 表示能否自动缩放。
        bool _scaleMode = false;

        bool _mouseLeftButtonDown = false;
        bool _dragStarted = false;
        WorkspaceItem _dragStartedWorkspaceItem;
        Point _dragStartedMousePosition;
        Point _dragStartedItemPosition;

        bool _mouseDoubleClicked = false;

        double _areaWidth;
        double _areaHeight;

        List<ItemInfo> _itemInfo = new List<ItemInfo>();

        List<WorkspaceItem> _itemsInitialOrder = new List<WorkspaceItem>();
        WorkspaceItem _itemMaximum;

        Canvas _canvas;

        AnimationHelper _animationHelper = new AnimationHelper();


        //HashSet<DoubleAnimation> _animations = new HashSet<DoubleAnimation>();

        //Dictionary<WorkspaceItem, Point> _animatedItemDestinations = new Dictionary<WorkspaceItem, Point>();

        public WorkspaceItemsControl()
        {
            Items.SortDescriptions.Add(new SortDescription("Y", ListSortDirection.Ascending));
            Items.SortDescriptions.Add(new SortDescription("X", ListSortDirection.Ascending));

            AddHandler(WorkspaceItem.MaximumChangedEvent, new MaximumChangedHandler(WorkspaceItem_MaximumChangedHandler));
        }

        void WorkspaceItem_MaximumChangedHandler(object sender, MaximumChangedEventArgs e)
        {
            Debug.WriteLine("WorkspaceItemsControl_WorkspaceItem_MaximumChangedHandler. e.Maximum: " + e.Maximum + " ((WorkspaceItem)e.Source).Title: " + ((WorkspaceItem)e.Source).Title);


            WorkspaceItem wi = (WorkspaceItem)e.Source;

            if (_animationHelper.HasActiveClock())
            {
                wi.SetMaximum(!e.Maximum, false);
                return;
            }


            if (TileMode)
            {
                wi.SetMaximum(true, false);
                MaximizeItem(wi);
                _itemMaximum = wi;
                TileMode = false;
            }
            else
            {
                if (object.ReferenceEquals(_itemMaximum, wi))
                {
                    wi.SetMaximum(false, false);
                    ScaleItems();
                }
                else
                {
                    wi.SetMaximum(true, false);
                    _itemMaximum.SetMaximum(false, false);
                    MaximizeItem(wi);
                    _itemMaximum = wi;
                }
            }

        }

        public bool TileMode
        {
            get { return (bool)GetValue(TileModeProperty); }
            set { SetValue(TileModeProperty, value); }
        }

        Canvas CanvasElement
        {
            get { return _canvas; }
            set
            {
                if (_canvas != null)
                {
                    _canvas.SizeChanged -= _canvas_SizeChanged;
                }

                _canvas = value;

                if (_canvas != null)
                {
                    _canvas.SizeChanged += _canvas_SizeChanged;
                }
            }
        }

        public override void OnApplyTemplate()
        {
            CanvasElement = GetTemplateChild("PART_Canvas") as Canvas;

            base.OnApplyTemplate();
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("OnItemsChanged. e.NewItems.Count: " + (e.NewItems == null ? "null." : e.NewItems.Count.ToString() + ".") +
                " e.OldItems: " + (e.OldItems == null ? "null." : e.OldItems.Count.ToString() + ".") + " Count: " + Items.Count + ".");

            //for (int i = 0; i < Items.Count; i++)
            //{
            //    Debug.WriteLine("Item " + i + ". X: " + ((WorkspaceItem)Items[i]).X + " Y: " + ((WorkspaceItem)Items[i]).Y);
            //}

            CheckIsScaleMode();

            if (_scaleMode)
            {
                Debug.WriteLine("CheckIsScaleMode: scaleMode.");

                _itemInfo.Clear();
                _itemsInitialOrder.Clear();

                WorkspaceItem item;

                for (int i = 0; i < Items.Count; i++)
                {
                    item = (WorkspaceItem)Items[i];

                    _itemInfo.Add(new ItemInfo
                    {
                        position = new Point(item.X, item.Y),
                        size = new Size(item.Width, item.Height),
                        item = item
                    });

                    _itemsInitialOrder.Add(item);
                }
            }
            else
            {
                Debug.WriteLine("CheckIsScaleMode: false.");
            }

            base.OnItemsChanged(e);
        }

        //
        // 多个鼠标设备 可能触发的事件过程：
        // 1. Down1 Up1 DoubleClick Down2    Up2
        // 2. Down DoubleClick Down    Up    Up 
        // 3. Down    Down   Up   Up
        // DoubleClick 一定先于 MouseDown 发生。
        // 
        // 以下事件序列：
        // MouseDown  MouseMove  DoubleClick MouseDown
        // 
        // 
        // MouseDown DoubleClick MouseDown


        // 这是“开”操作，涉及：
        // 1. 拖动
        //    1.1 _mouseLeftButtonDown
        //    1.2 MouseCapture
        //    1.3 MouseMove
        //    1.4 _dragStarted
        //    1.5 Items
        // 
        // 2. 双击
        //    2.1 Items
        // 
        // 双击事件不在此处处理。_mouseDoubleClicked 为真时，回退“拖动”操作。
        // 以上两操作互斥。
        // 
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Debug.WriteLine("OnMouseLeftButtonDown. e.Source: " + e.Source);

            // 第一次设置 _mouseLeftButtonDown 有效。（Mouse1Down Mouse2Down）
            // 不论有多少 MouseDevices，Mouse 只有一个。
            if (!_mouseLeftButtonDown && !_animationHelper.HasActiveClock())
            {
                Point position = e.GetPosition(CanvasElement);
                WorkspaceItem wi = GetSelectedItemWithHeader(position);

                if (wi != null)
                {
                    wi.NotifyMouseNotLeave();
                    CanvasElement.CaptureMouse();

                    _mouseLeftButtonDown = true;
                    
                    //_dragStarted = true;

                    //if (wi.HasAnimatedProperties)
                    //{
                    //    Debug.WriteLine("  HasAnimatedProperties.");

                    //    double x = wi.X;
                    //    double y = wi.Y;
                    //    double width = wi.Width;
                    //    double height = wi.Height;

                    //    wi.BeginAnimation(WorkspaceItem.XProperty, null);
                    //    wi.BeginAnimation(WorkspaceItem.YProperty, null);
                    //    wi.BeginAnimation(WorkspaceItem.WidthProperty, null);
                    //    wi.BeginAnimation(WorkspaceItem.HeightProperty, null);

                    //    wi.X = x;
                    //    wi.Y = y;
                    //    wi.Width = width;
                    //    wi.Height = height;
                    //}

                    _dragStartedWorkspaceItem = wi;
                    _dragStartedItemPosition = new Point(wi.X, wi.Y);
                    _dragStartedMousePosition = position;

                    Panel.SetZIndex(wi, 1);
                }

            }

            // 取消之前的操作。
            if (_mouseDoubleClicked)
            {
                Mouse.Capture(null);

                if (_dragStartedWorkspaceItem != null)
                {
                    Panel.SetZIndex(_dragStartedWorkspaceItem, 0);
                }

                _dragStartedWorkspaceItem = null;
                _mouseLeftButtonDown = false;

                _mouseDoubleClicked = false;
            }

            Debug.WriteLine("  After MouseLeftButtonDown. _mouseLeftButtonDown: " + _mouseLeftButtonDown + " _mouseDoubleClicked: " + _mouseDoubleClicked);

            base.OnMouseLeftButtonDown(e);
        }

        // 这是过渡状态。
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseLeftButtonDown)
            {
                Point position = e.GetPosition(CanvasElement);

                if (!_dragStarted)
                {
                    if ((Math.Abs(position.X - _dragStartedMousePosition.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                        (Math.Abs(position.Y - _dragStartedMousePosition.Y) > SystemParameters.MinimumVerticalDragDistance))
                    {
                        _dragStartedWorkspaceItem.X = _dragStartedItemPosition.X + (position.X - _dragStartedMousePosition.X);
                        _dragStartedWorkspaceItem.Y = _dragStartedItemPosition.Y + (position.Y - _dragStartedMousePosition.Y);

                        _dragStarted = true;
                    }
                }
                else
                {
                    _dragStartedWorkspaceItem.X = _dragStartedItemPosition.X + (position.X - _dragStartedMousePosition.X);
                    _dragStartedWorkspaceItem.Y = _dragStartedItemPosition.Y + (position.Y - _dragStartedMousePosition.Y);
                }
            }

            base.OnMouseMove(e);
        }

        // 这是“关”操作。
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            Debug.WriteLine("OnMouseLeftButtonUp. e.Source: " + e.Source);
            Debug.WriteLine("  OnMouseLeftbuttonUp. _dragStarted: " + _dragStarted + " MouseLeftButtonDown: " + _mouseLeftButtonDown);

            if (_mouseLeftButtonDown)
            {
                Mouse.Capture(null);
                Panel.SetZIndex(_dragStartedWorkspaceItem, 0);

                //if (_mouseDoubleClicked)
                //{
                //    _mouseDoubleClicked = false;
                //    return;
                //}

                if (_dragStarted)
                {
                    WorkspaceItem wi = GetSelectedItem(e.GetPosition(CanvasElement), _dragStartedWorkspaceItem);

                    if (TileMode)
                    {
                        if (wi != null)
                        {
                            Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. TileMode. wi: " + wi.Title);

                            for (int i = 0; i < _itemInfo.Count; i++)
                            {
                                if (object.ReferenceEquals(wi, _itemInfo[i].item))
                                {
                                    //(_itemInfo[i].item) = _dragStartedWorkspaceItem;

                                    _itemInfo[i] = new ItemInfo
                                    {
                                        position = _itemInfo[i].position,
                                        size = _itemInfo[i].size,
                                        item = _dragStartedWorkspaceItem
                                    };
                                }
                                else if (object.ReferenceEquals(_dragStartedWorkspaceItem, _itemInfo[i].item))
                                {
                                    _itemInfo[i] = new ItemInfo
                                    {
                                        position = _itemInfo[i].position,
                                        size = _itemInfo[i].size,
                                        item = wi
                                    };
                                }
                            }

                            SwapItemWidthDragStartedItem(wi);
                        }
                        else
                        {
                            Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. TileMode. wi: null");

                            //DoubleAnimation animationX = new DoubleAnimation();
                            //animationX.To = _dragStartedItemPosition.X;
                            //animationX.Duration = new Duration(TimeSpan.FromSeconds(2));
                            //animationX.Completed += animation_Completed;

                            //AnimationClock clock = animationX.CreateClock();

                            //Debug.WriteLine("clock.Timeline: " + clock.Timeline);


                            //DoubleAnimation animationY = new DoubleAnimation();
                            //animationY.To = _dragStartedItemPosition.Y;
                            //animationY.Duration = new Duration(TimeSpan.FromSeconds(2));
                            //animationY.Completed += animation_Completed;

                            //if (_animatedItemDestinations.ContainsKey(_dragStartedWorkspaceItem))
                            //{
                            //    _animatedItemDestinations[_dragStartedWorkspaceItem] = _dragStartedItemPosition;
                            //}
                            //else
                            //{
                            //    _animatedItemDestinations.Add(_dragStartedWorkspaceItem, _dragStartedItemPosition);
                            //}


                            //_dragStartedWorkspaceItem.ApplyAnimationClock

                            //_dragStartedWorkspaceItem.BeginAnimation(WorkspaceItem.XProperty, animationX);
                            //_dragStartedWorkspaceItem.BeginAnimation(WorkspaceItem.YProperty, animationY);

                            //_dragStartedWorkspaceItem.X = _dragStartedItemPosition.X;
                            //_dragStartedWorkspaceItem.Y = _dragStartedItemPosition.Y;

                            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.XProperty, _dragStartedItemPosition.X);
                            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.YProperty, _dragStartedItemPosition.Y);
                        }
                    }
                    // Maximized Item
                    else
                    {
                        if (wi != null)
                        {
                            Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. !TileMode. wi: " + wi.Title);

                            if (!object.ReferenceEquals(wi, _itemMaximum) && !object.ReferenceEquals(_dragStartedWorkspaceItem, _itemMaximum))
                            {
                                Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. !TileMode. two collapsed items.");
                                //_dragStartedWorkspaceItem.X = _dragStartedItemPosition.X;
                                //_dragStartedWorkspaceItem.Y = _dragStartedItemPosition.Y;

                                _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.XProperty, _dragStartedItemPosition.X);
                                _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.YProperty, _dragStartedItemPosition.Y);
                            }
                            else
                            {
                                // 目标是  _itemMaximum。
                                if (object.ReferenceEquals(wi, _itemMaximum))
                                {
                                    wi.SetMaximum(false, false);
                                    _dragStartedWorkspaceItem.SetMaximum(true, false);
                                    MaximizeItem(_dragStartedWorkspaceItem);
                                    _itemMaximum = _dragStartedWorkspaceItem;
                                    Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. !TileMode. Destination: _itemMaximum. _itemMaximum: " + _itemMaximum.Title);

                                }
                                // 源是 _itemMaximum。
                                else
                                {
                                    _itemMaximum.SetMaximum(false, false);
                                    wi.SetMaximum(true, false);

                                    MaximizeItem(wi);
                                    _itemMaximum = wi;
                                    Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. !TileMode. Source: _itemMaximum. _itemMaximum: " + _itemMaximum.Title);
                                }

                            }
                        }
                        else
                        {
                            //_dragStartedWorkspaceItem.X = _dragStartedItemPosition.X;
                            //_dragStartedWorkspaceItem.Y = _dragStartedItemPosition.Y;

                            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.XProperty, _dragStartedItemPosition.X);
                            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.YProperty, _dragStartedItemPosition.Y);

                            Debug.WriteLine("  WorkspaceItemsControl.OnMouseLeftButtonUp. !TileMode. wi: null");
                        }
                    }

                    _dragStarted = false;
                }

                _dragStartedWorkspaceItem = null;
                _mouseLeftButtonDown = false;
            }

            base.OnMouseLeftButtonUp(e);
        }

        void animation_Completed(object sender, EventArgs e)
        {
            Debug.WriteLine("animation_Completed. sender: " + sender);

            //AnimationClock clock = sender as AnimationClock;

        }

        // 这是独立事件处理程序。本操作影响 _mouseDoubleClicked 状态。
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            Debug.WriteLine("MouseDoubleClick. e.Source: " + e.Source);
            Point position = e.GetPosition(CanvasElement);
            WorkspaceItem wi = GetSelectedItemWithHeader(position);

            // 简单起见，双击事件以最后一次坐标为准。
            if (e.ChangedButton == MouseButton.Left && wi != null && !_animationHelper.HasActiveClock())
            {
                Debug.WriteLine("  WorkspaceItemsControl.OnMouseDoubleClick. Item: " + wi.Title);

                _mouseDoubleClicked = true;

                if (TileMode)
                {
                    wi.SetMaximum(true, false);
                    MaximizeItem(wi);
                    _itemMaximum = wi;

                    TileMode = false;
                }
                else
                {
                    if (object.ReferenceEquals(wi, _itemMaximum))
                    {
                        wi.SetMaximum(false, false);
                        ScaleItems();
                        TileMode = true;
                    }
                    else
                    {
                        wi.SetMaximum(true, false);
                        _itemMaximum.SetMaximum(false, false);
                        MaximizeItem(wi);
                        _itemMaximum = wi;
                    }
                }
            }
            else
            {
                Debug.WriteLine("  WorkspaceItemsControl.OnMouseDoubleClick. Item: null");
            }

            base.OnMouseDoubleClick(e);
        }

        void _canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine("_canvas_SizeChanged. ActualWidth: " + CanvasElement.ActualWidth + " ActualHeight: " + CanvasElement.ActualHeight +
                " DesireSize: " + CanvasElement.DesiredSize + " RenderSize: " + CanvasElement.RenderSize);


            if (TileMode)
            {
                _animationHelper.RemoveAll();

                ScaleItems();
            }
            else
            {
                _animationHelper.RemoveAll();

                MaximizeItem(_itemMaximum);
            }

        }

        void CheckIsScaleMode()
        {
            double areaWidth = 0;
            double areaHeight = 0;
            double ItemAreasCount = 0;

            // 检查
            // 1. 是否重叠。
            // 2. 是否有间隙。
            foreach (var item in Items)
            {
                WorkspaceItem wi = item as WorkspaceItem;

                if (wi == null)
                {
                    return;
                }

                // X、Y 必须非负
                if (wi.X < 0 || wi.Y < 0)
                {
                    return;
                }

                // 必须设置 Width 和 Height 属性。
                if (double.IsNaN(wi.Width) || double.IsNaN(wi.Height))
                {
                    return;
                }

                foreach (var i in Items)
                {
                    WorkspaceItem wi2 = i as WorkspaceItem;

                    if (wi2 == null)
                    {
                        return;
                    }

                    if (object.ReferenceEquals(wi, wi2))
                    {
                        continue;
                    }

                    // X、Y 必须非负
                    if (wi2.X < 0 || wi2.Y < 0)
                    {
                        return;
                    }

                    // 必须设置 Width 和 Height 属性。
                    if (double.IsNaN(wi2.Width) || double.IsNaN(wi2.Height))
                    {
                        return;
                    }

                    Rect rect = new Rect(wi.X, wi.Y, wi.Width, wi.Height);
                    rect.Intersect(new Rect(wi2.X, wi2.Y, wi2.Width, wi2.Height));

                    if (!rect.IsEmpty && rect.Width > 0 && rect.Height > 0)
                    {
                        return;
                    }
                }

                if (wi.X + wi.Width > areaWidth)
                {
                    areaWidth = wi.X + wi.Width;
                }

                if (wi.Y + wi.Height > areaHeight)
                {
                    areaHeight = wi.Y + wi.Height;
                }

                ItemAreasCount += wi.Width * wi.Height;
            }

            if (areaWidth * areaHeight == ItemAreasCount)
            {
                _scaleMode = true;

                _areaWidth = areaWidth;
                _areaHeight = areaHeight;
            }

        }

        WorkspaceItem GetSelectedItem(Point position, WorkspaceItem itemExcluded)
        {
            WorkspaceItem item = null;

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (object.ReferenceEquals(Items[i], itemExcluded))
                {
                    continue;
                }

                WorkspaceItem wi = Items[i] as WorkspaceItem;


                if (wi != null)
                {
                    double x = wi.X;
                    double y = wi.Y;
                    double width = wi.ActualWidth;
                    double height = wi.ActualHeight;

                    //Debug.WriteLine("Title: " + wi.Title + " IsLoaded: " + wi.IsLoaded + " Width: " + wi.Width);
                    //Debug.WriteLine(string.Format("x: {0} y: {1} width: {2} height: {3}", x, y, width, height));

                    if (position.X >= x && position.X <= x + width &&
                        position.Y >= y && position.Y <= y + height)
                    {
                        //Debug.WriteLine("Find item: " + wi.Title);
                        return wi;
                    }
                }
            }

            return item;
        }

        WorkspaceItem GetSelectedItemWithHeader(Point position)
        {
            WorkspaceItem item = null;

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                WorkspaceItem wi = Items[i] as WorkspaceItem;


                if (wi != null)
                {
                    double x = wi.X;
                    double y = wi.Y;
                    double width = wi.ActualWidth;
                    double height = wi.ActualHeight;

                    //Debug.WriteLine("Title: " + wi.Title + " IsLoaded: " + wi.IsLoaded + " Width: " + wi.Width);
                    //Debug.WriteLine(string.Format("x: {0} y: {1} width: {2} height: {3}", x, y, width, height));

                    if (position.X >= x && position.X <= x + width &&
                        position.Y >= y && position.Y <= y + wi.TitleHeight)
                    {
                        //Debug.WriteLine("Find item with header: " + wi.Title);
                        return wi;
                    }
                }
            }

            return item;
        }

        void SwapItemWidthDragStartedItem(WorkspaceItem item)
        {
            Point tempPoint = new Point(item.X, item.Y);
            Size tempSize = new Size(item.Width, item.Height);

            //item.X = _dragStartedItemPosition.X;
            //item.Y = _dragStartedItemPosition.Y;
            //item.Width = _dragStartedWorkspaceItem.Width;
            //item.Height = _dragStartedWorkspaceItem.Height;

            _animationHelper.BeginAnimation(item, WorkspaceItem.XProperty, _dragStartedItemPosition.X);
            _animationHelper.BeginAnimation(item, WorkspaceItem.YProperty, _dragStartedItemPosition.Y);
            _animationHelper.BeginAnimation(item, WorkspaceItem.WidthProperty, _dragStartedWorkspaceItem.Width);
            _animationHelper.BeginAnimation(item, WorkspaceItem.HeightProperty, _dragStartedWorkspaceItem.Height);

            //_dragStartedWorkspaceItem.X = tempPoint.X;
            //_dragStartedWorkspaceItem.Y = tempPoint.Y;
            //_dragStartedWorkspaceItem.Width = tempSize.Width;
            //_dragStartedWorkspaceItem.Height = tempSize.Height;

            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.XProperty, tempPoint.X);
            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.YProperty, tempPoint.Y);
            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.WidthProperty, tempSize.Width);
            _animationHelper.BeginAnimation(_dragStartedWorkspaceItem, WorkspaceItem.HeightProperty, tempSize.Height);
        }

        void ScaleItems()
        {
            // 内边距 8
            // Items 外边距 10

            double totalWidth = CanvasElement.ActualWidth - 16;
            double totalHeight = CanvasElement.ActualHeight - 16;
            double xRatio = totalWidth / _areaWidth;
            double yRatio = totalHeight / _areaHeight;

            //Debug.WriteLine("ScaleItems. totalWidth: " + totalWidth + " totalHeight: " + totalHeight + " xRatio: " + xRatio + " yRatio: " + yRatio);

            foreach (var item in _itemInfo)
            {
                //if (item.item.HasAnimatedProperties)
                //{
                //    item.item.BeginAnimation(WorkspaceItem.XProperty, null);
                //    item.item.BeginAnimation(WorkspaceItem.YProperty, null);
                //    item.item.BeginAnimation(WorkspaceItem.WidthProperty, null);
                //    item.item.BeginAnimation(WorkspaceItem.HeightProperty, null);
                //}

                double x = item.position.X * xRatio + 8;
                double y = item.position.Y * yRatio + 8;

                double newWidth = item.size.Width * xRatio;
                double newHeight = item.size.Height * yRatio;

                newWidth = newWidth < 0 ? 0 : newWidth;
                newHeight = newHeight < 0 ? 0 : newHeight;



                //Debug.WriteLine("ScaleItems. Item title: " + item.item.Title +
                //    " X: " + item.item.X + " Y: " + item.item.Y +
                //    " Width: " + item.item.Width + " Height: " + item.item.Height);

                //
                // 修正外边距
                //
                //Debug.WriteLine("Item title: " + item.item.Title + " item.item.X - 8 : " + (item.item.X - 8));

                if (x - 8 > 1)
                {
                    //Debug.WriteLine("  True");
                    x += 5;

                    newWidth -= 5;
                    newWidth = newWidth < 0 ? 0 : newWidth;
                }

                //Debug.WriteLine("Item title: " + item.item.Title + " Abs: " + (item.item.X + item.item.Width - totalWidth));

                if (totalWidth + 8 - x - newWidth > 1)
                {
                    newWidth -= 5;
                    newWidth = newWidth < 0 ? 0 : newWidth;
                }

                if (y - 8 > 1)
                {
                    y += 5;

                    newHeight -= 5;
                    newHeight = newHeight < 0 ? 0 : newHeight;
                }

                if (totalHeight + 8 - y - newHeight > 1)
                {
                    newHeight -= 5;
                    newHeight = newHeight < 0 ? 0 : newHeight;
                }

                item.item.TitleHeight = 32;

                _animationHelper.BeginAnimation(item.item, WorkspaceItem.XProperty, x);
                _animationHelper.BeginAnimation(item.item, WorkspaceItem.YProperty, y);
                _animationHelper.BeginAnimation(item.item, WorkspaceItem.WidthProperty, newWidth);
                _animationHelper.BeginAnimation(item.item, WorkspaceItem.HeightProperty, newHeight);
            }

            TileMode = true;
        }

        void MaximizeItem(WorkspaceItem wi)
        {
            //if (TileMode)
            //{
            //wi.X = wi.Y = 8;

            double newWidth = CanvasElement.ActualWidth - 16 - 255 - 10;
            double newHeight = CanvasElement.ActualHeight - 16;

            newWidth = newWidth < 0 ? 0 : newWidth;
            newHeight = newHeight < 0 ? 0 : newHeight;

            wi.TitleHeight = 32;

            _animationHelper.BeginAnimation(wi, WorkspaceItem.XProperty, 8);
            _animationHelper.BeginAnimation(wi, WorkspaceItem.YProperty, 8);
            _animationHelper.BeginAnimation(wi, WorkspaceItem.WidthProperty, newWidth);
            _animationHelper.BeginAnimation(wi, WorkspaceItem.HeightProperty, newHeight);

            WorkspaceItem item;
            double startX = CanvasElement.ActualWidth < 255 + 16 ? 8 : 8 + newWidth + 10;
            double startY = 8;

            for (int i = 0; i < _itemsInitialOrder.Count; i++)
            {
                item = _itemsInitialOrder[i];
                if (!object.ReferenceEquals(wi, item))
                {
                    //item.X = startX;
                    //item.Y = startY;

                    //item.Width = 255;
                    //item.Height = 40;

                    item.TitleHeight = 40;

                    _animationHelper.BeginAnimation(item, WorkspaceItem.XProperty, startX);
                    _animationHelper.BeginAnimation(item, WorkspaceItem.YProperty, startY);
                    _animationHelper.BeginAnimation(item, WorkspaceItem.WidthProperty, 255);
                    _animationHelper.BeginAnimation(item, WorkspaceItem.HeightProperty, 40);

                    startY += 50;
                }
            }

            //}
            //else
            //{
            //    if (_itemMaximum == wi)
            //    {
            //        return;
            //    }



            //}
        }

    }
}
