using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace Simple_Marksmans.Utils.PermaShow
{
    internal class PermaShow
    {
        private readonly List<ItemData> _permaShowItems = new List<ItemData>();
        private readonly List<SeparatorData> _separators = new List<SeparatorData>();
        private readonly List<SeparatorData> _underlines = new List<SeparatorData>();
        private readonly List<MenuItem> _menuItems = new List<MenuItem>();
 
        private readonly Text _headerText;

        public bool IsMoving { get; private set; }
        private readonly Vector2 _defaultPosition = new Vector2(190, 90);

        public Vector2 Position { get; set; }
        
        public ColorBGRA BackgroundColor { get; set; } = new ColorBGRA(14, 19, 20, 215);
        public ColorBGRA SeparatorColor { get; set; } = new ColorBGRA(16, 29, 29, 255);
        public ColorBGRA EnabledUnderlineColor { get; set; } = new ColorBGRA(173, 255, 47, 255);
        public ColorBGRA DisabledUnderlineColor { get; set; } = new ColorBGRA(255, 0, 0, 255);
        public ColorBGRA TextColor { get; set; } = new ColorBGRA(109, 101, 64, 255);

        public static Menu Menu { get; set; }

        public bool Enabled
        {
            get
            {
                return Menu?["Enable"] != null && Menu["Enable"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (Menu?["Enable"] == null)
                    return;

                Menu["Enable"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public int DefaultSpacing
        {
            get { return Menu?["Spacing"]?.Cast<Slider>().CurrentValue ?? 0; }
            set
            {
                if (Menu?["Spacing"] == null)
                    return;

                Menu["Spacing"].Cast<Slider>().CurrentValue = value;
            }
        }

        public int Opacity
        {
            get { return Menu?["Opacity"]?.Cast<Slider>().CurrentValue ?? 0; }
            set
            {
                if (Menu?["Opacity"] == null)
                    return;

                Menu["Opacity"].Cast<Slider>().CurrentValue = value;
            }
        }

        public PermaShow(string headerName, Vector2 pos)
        {
            _headerText = new Text(headerName, 19, (int)_defaultPosition.X, (int)_defaultPosition.Y, TextColor);

            Position = pos;

            Core.DelayAction(() =>
            {
                Menu = MenuManager.Menu.AddSubMenu(headerName, headerName);
                CreateMenu();

                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
                Drawing.OnEndScene += Drawing_OnDraw;

                Game.OnTick += Game_OnTick;
                Game.OnWndProc += Game_OnWndProc;

            }, 2500);
        }
        
        private void CreateMenu()
        {
            Menu.Add("Enable", new CheckBox("Enable PermaShow", false));
            Menu.Add("Spacing", new Slider("Spacing", 25, 10, 50)).OnValueChange += delegate { UpdatePositions(); };
            Menu.Add("Opacity", new Slider("Opacity", 255, 0, 255)).OnValueChange += delegate { UpdatePositions(); };

            UpdatePositions();
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (!Enabled)
               return;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (args.Msg)
            {
                case (uint) WindowMessages.LeftButtonDown:
                    if (IsPositionOnPermaShow(Game.CursorPos2D))
                        IsMoving = true;
                    break;
                case (uint) WindowMessages.LeftButtonUp:
                    IsMoving = false;
                    break;
            }
        }

        private void Game_OnTick(EventArgs args)
        {
            if(!Enabled)
                return;
            
            if (IsMoving)
            {
                Position = Game.CursorPos2D;
                UpdatePositions();
            }
            /*
            foreach (var re in _permaShowItems.Where(x => x.Type == typeof (BoolItemData)))
            {
                var item = _menuItems.FirstOrDefault(where => where.ItemName == re.ItemName.Message);

                if (item == null)
                    return;

                var index =
                    _permaShowItems.Where(x => x.ItemType == ItemTypes.Bool)
                        .ToList()
                        .FindIndex(data => re.ItemName == data.ItemName);

                if ( && re.ItemValue.Message.Contains("Disabled"))
                {
                    var textWidth = re.ItemValue.GetTextRectangle().Width;
                    re.ItemValue.Message = "[ ✓ ] Enabled";
                    _underlines.ToArray()[index].Color = new ColorBGRA(173, 255, 47, 255);
                    _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                                  textWidth +
                                                                  re.ItemValue.GetTextRectangle().Width;
                }
                else if (!menuData[item.MenuItemName] && re.ItemValue.Message.Contains("Enabled"))
                {
                    var textWidth = re.ItemValue.GetTextRectangle().Width;
                    re.ItemValue.Message = "[ X ] Disabled";
                    _underlines.ToArray()[index].Color = new ColorBGRA(255, 0, 0, 255);
                    _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                                  textWidth +
                                                                  re.ItemValue.GetTextRectangle().Width;
                }

            }*/

            foreach (var re in _permaShowItems.Where(x => x.Type == typeof(MenuItem)))
            {
                var item = _menuItems.FirstOrDefault(where => where.ItemName == re.ItemName.Message);

                if (item == null)
                    return;

                var menuData = MenuManager.MenuValues;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (re.ItemType)
                {
                    case ItemTypes.Bool:

                        var index = _permaShowItems.Where(x=>x.ItemType == ItemTypes.Bool).ToList().FindIndex(data => re.ItemName == data.ItemName);

                        if (menuData[item.MenuItemName] && re.ItemValue.Message.Contains("Disabled"))
                        {
                            var textWidth = re.ItemValue.GetTextRectangle().Width;
                            re.ItemValue.Message = "[ ✓ ] Enabled";
                            _underlines.ToArray()[index].Color = new ColorBGRA(173, 255, 47, 255);
                            _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                                          textWidth +
                                                                          re.ItemValue.GetTextRectangle().Width;
                        }
                        else if (!menuData[item.MenuItemName] && re.ItemValue.Message.Contains("Enabled"))
                        {
                            var textWidth = re.ItemValue.GetTextRectangle().Width;
                            re.ItemValue.Message = "[ X ] Disabled";
                            _underlines.ToArray()[index].Color = new ColorBGRA(255, 0, 0, 255);
                            _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                                          textWidth +
                                                                          re.ItemValue.GetTextRectangle().Width;
                        }
                        break;
                    case ItemTypes.Integer:
                        if (menuData[item.MenuItemName, true] != Convert.ToInt32(re.ItemValue.Message.Remove(0, 8)))
                        {
                            re.ItemValue.Message = "Value : " + menuData[item.MenuItemName, true];
                        }
                        break;
                }
            }
        }

        private int GetMaxItemNameTextLength()
        {
            var itemNameTextLength = 0;

            foreach (var item in _permaShowItems)
            {
                var itemNameTextWidth = item.ItemName.GetTextRectangle().Width;

                if (itemNameTextLength == 0 || itemNameTextLength < itemNameTextWidth)
                {
                    itemNameTextLength = itemNameTextWidth;
                }
            }
            return itemNameTextLength;
        }

        private int GetMaxItemValueTextLength()
        {
            var itemValueTextLength = 0;

            foreach (var item in _permaShowItems)
            {
                var itemValueTextWidth = item.ItemValue.GetTextRectangle().Width;

                if (itemValueTextLength == 0 || itemValueTextLength < itemValueTextWidth)
                {
                    itemValueTextLength = itemValueTextWidth;
                }
            }
            return itemValueTextLength;
        }

        private int GetMaxTextLength()
        {
            var a = (int)(GetMaxItemNameTextLength() + GetMaxItemValueTextLength() + DefaultSpacing*1.25f);
            var b = _headerText.GetTextRectangle().Width;

            return a > b ? a : b;
        }
        
        private void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || Opacity == 0)
                return;

            if (Drawing.Direct3DDevice.IsDisposed || CountItems() == 0)// || !Enabled)
                return;

            var lastSeparator = _separators.Last();
            var width = Position.X + GetMaxTextLength() + DefaultSpacing*2 - Position.X;

            var background = new Rectangle((int) width + 8,
                new[]
                {
                    new Vector2(Position.X + width/2, Position.Y),
                    new Vector2(Position.X + width/2, lastSeparator.Positions[0].Y + 5)
                },
                new ColorBGRA(BackgroundColor.R, BackgroundColor.G,BackgroundColor.B, (byte)Opacity));
            background.Draw();
            
            _headerText.Font.DrawText(null, _headerText.Message, (int) Position.X + DefaultSpacing, (int) Position.Y,
                new ColorBGRA(_headerText.Color.R, _headerText.Color.G, _headerText.Color.B, (byte)Opacity));
            
            var separator = new Rectangle(3,
                new[]
                {
                    new Vector2(Position.X, Position.Y + _headerText.Height*1.15f),
                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing*2,
                        Position.Y + _headerText.Height*1.15f)
                }, new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte)Opacity));
            separator.Draw();

            var straightLine = new Rectangle(2,
                new[]
                {
                    new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2,
                        Position.Y + _headerText.Height*1.85f),
                    new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2, lastSeparator.Positions[0].Y)
                }, new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte)Opacity));
            straightLine.Draw();

            foreach (var re in _permaShowItems)
            {
                re.ItemName.Font.DrawText(null, re.ItemName.Message, re.ItemName.X, re.ItemName.Y,
                    new ColorBGRA(re.ItemName.Color.R, re.ItemName.Color.G, re.ItemName.Color.B, (byte) Opacity));
                re.ItemValue.Font.DrawText(null, re.ItemValue.Message, re.ItemValue.X, re.ItemValue.Y,
                    new ColorBGRA(re.ItemValue.Color.R, re.ItemValue.Color.G, re.ItemValue.Color.B, (byte) Opacity));
            }

            foreach (var rectangle in _separators.Select(sep => new Rectangle((int) sep.Width, sep.Positions,
                new ColorBGRA(sep.Color.R, sep.Color.G, sep.Color.B, (byte) Opacity))))
            {
                rectangle.Draw();
            }

            foreach (var rectangle in _underlines.Select(underline => new Rectangle((int) underline.Width, underline.Positions,
                new ColorBGRA(underline.Color.R, underline.Color.G, underline.Color.B, (byte) Opacity))))
            {
                rectangle.Draw();
            }
        }

        private void UpdatePositions()
        {
            var itenNameXPosition = (int)Position.X + DefaultSpacing;
            var itemValueXPosition = (int)(Position.X + GetMaxItemNameTextLength() + DefaultSpacing * 2.5f);

            foreach (var re in _permaShowItems)
            {
                var index = _permaShowItems.IndexOf(re);
                var lastItem = _permaShowItems[index == 0 ? 0 : index - 1];
                var yPosition = index == 0
                    ? (int)(Position.Y + _headerText.Height * 2f)
                    : (int)(Position.Y + _headerText.Height * 2f) + (int)((lastItem.ItemName.Height + 10) * index);

                _permaShowItems[index].ItemName.X = itenNameXPosition;
                _permaShowItems[index].ItemName.Y = yPosition;
                _permaShowItems[index].ItemValue.X = itemValueXPosition;
                _permaShowItems[index].ItemValue.Y = yPosition;
            }

            foreach (var sep in _separators)
            {
                var index = _separators.IndexOf(sep);
                var xPositon = Position.X + GetMaxTextLength() + DefaultSpacing * 2;
                float yPosition;

                switch (index)
                {
                    case 0:
                        yPosition = Position.Y + _headerText.Height * 1.85f;
                        break;
                    case 1:
                        yPosition = Position.Y + _headerText.Height * 2f + _permaShowItems[0].ItemName.Height + 5;
                        break;
                    default:
                        yPosition =
                            _permaShowItems[index - 2].ItemName.Y +
                            _permaShowItems[index - 2].ItemName.Height +
                            10 +
                            _permaShowItems[index - 2].ItemName.Height +
                            5;
                        break;
                }

                _separators[index].Positions = new[] { new Vector2(Position.X, yPosition), new Vector2(xPositon, yPosition) };
                Core.DelayAction(()=>_separators[index].Color = SeparatorColor, 500);
            }


            foreach (var underline in _underlines)
            {
                var permaShowItem = _permaShowItems.Where(x => x.ItemType == ItemTypes.Bool).ToArray()[_underlines.IndexOf(underline)];
                var x1Position = permaShowItem.ItemValue.X;
                var x2Position = itemValueXPosition + permaShowItem.ItemValue.GetTextRectangle().Width;
                var yPosition = permaShowItem.ItemValue.Y + permaShowItem.ItemValue.Height;

                _underlines[_underlines.IndexOf(underline)].Positions = new[] { new Vector2(x1Position + 25, yPosition), new Vector2(x2Position, yPosition) };
            }
        }

        private int CountItems()
        {
            return _permaShowItems.Count;
        }

        public T AddItem<T>(string text, T value) where T : IPermaShowItem
        {
            if (_permaShowItems.FirstOrDefault() == null)
            {
                if (value.GetType() == typeof (BoolItemData))
                {
                    var data = value as BoolItemData;

                    if (!string.IsNullOrEmpty(data?.ItemName) && data.TextHeight > 1)
                    {
                        _separators.Add(new SeparatorData
                        {
                            Color = new ColorBGRA(16, 29, 29, 255),
                            Positions = new[] {new Vector2(200, 130), new Vector2(500, 130)},
                            Width = 2
                        });

                        var itemValue = new Text(data.Value ? "[ ✓ ] Enabled" : "[ X ] Disabled", data.TextHeight, 350,
                            135,
                            new ColorBGRA(109, 101, 64, 255));

                        _permaShowItems.Add(new ItemData
                        {
                            ItemType = ItemTypes.Bool,
                            ItemName =
                                new Text(data.ItemName, data.TextHeight, 215, 135, new ColorBGRA(109, 101, 64, 255)),
                            ItemValue = itemValue,
                            Type = typeof (BoolItemData)
                        });

                        _underlines.Add(new SeparatorData
                        {
                            Color = data.Value ? new ColorBGRA(173, 255, 47, 255) : new ColorBGRA(255, 0, 0, 255),
                            Positions =
                                new[]
                                {new Vector2(376, 150), new Vector2(350 + itemValue.GetTextRectangle().Width, 150)},
                            Width = 1
                        });

                        _separators.Add(new SeparatorData
                        {
                            Color = new ColorBGRA(16, 29, 29, 255),
                            Positions = new[] {new Vector2(200, 155), new Vector2(500, 155)},
                            Width = 2
                        });

                        var t = new BoolItemData(data.ItemName, data.Value, data.TextHeight);
                        t.OnValueChangeEvent += BoolItemData_OnValueChangeEvent;

                        return (T)Convert.ChangeType(t, typeof(BoolItemData));
                    }
                }
                if (value.GetType() == typeof (MenuItem))
                {
                    var data = value as MenuItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && data.TextHeight > 1)
                    {

                        var menu = MenuManager.MenuValues;

                        var itemName = new Text(data.ItemName, data.TextHeight, (int) Position.X + DefaultSpacing,
                            (int) (Position.Y + _headerText.Height*2f), TextColor);
                        var itemValue = new Text(menu[data.MenuItemName] ? "[ ✓ ] Enabled" : "[ X ] Disabled",
                            data.TextHeight,
                            (int) (Position.X + itemName.GetTextRectangle().Width + DefaultSpacing*2.5f),
                            (int) (Position.Y + _headerText.Height*2f),
                            TextColor);

                        _permaShowItems.Add(new ItemData
                        {
                            ItemType = ItemTypes.Bool,
                            ItemName = itemName,
                            ItemValue = itemValue,
                            Type = typeof (MenuItem)
                        });

                        _menuItems.Add(new MenuItem(data.ItemName, data.MenuItemName, data.ItemType, data.TextHeight));

                        _separators.Add(new SeparatorData
                        {
                            Color = SeparatorColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(Position.X, Position.Y + _headerText.Height*1.85f),
                                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing,
                                        Position.Y + _headerText.Height*1.85f)
                                },
                            Width = 2
                        });

                        _underlines.Add(new SeparatorData
                        {
                            Color = menu[data.MenuItemName] ? EnabledUnderlineColor : DisabledUnderlineColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(
                                        (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f),
                                        Position.Y + _headerText.Height*2f + data.TextHeight),
                                    new Vector2(
                                        (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) +
                                        itemValue.GetTextRectangle().Width + 27,
                                        Position.Y + _headerText.Height*2f + data.TextHeight)
                                },
                            Width = 1
                        });

                        _separators.Add(new SeparatorData
                        {
                            Color = SeparatorColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(Position.X, Position.Y + _headerText.Height*2f + data.TextHeight + 5),
                                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing,
                                        Position.Y + _headerText.Height*2f + data.TextHeight + 5)
                                },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(this, typeof(MenuItem));
                    }
                }
            }
            else
            {
                if (value.GetType() == typeof (MenuItem))
                {
                    var data = value as MenuItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && data.TextHeight > 1)
                    {
                        var menu = MenuManager.MenuValues;
                        var lastItem = _permaShowItems.Last();
                        Text itemName;
                        Text itemValue;

                        if (data.ItemType == ItemTypes.Bool)
                        {
                            itemName = new Text(data.ItemName, data.TextHeight, (int) Position.X + DefaultSpacing,
                                (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10), TextColor);
                            itemValue = new Text(menu[data.MenuItemName] ? "[ ✓ ] Enabled" : "[ X ] Disabled",
                                data.TextHeight,
                                (int) (Position.X + itemName.GetTextRectangle().Width + DefaultSpacing*2.5f),
                                (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10),
                                TextColor);

                            _permaShowItems.Add(new ItemData
                            {
                                ItemType = ItemTypes.Bool,
                                ItemName = itemName,
                                ItemValue = itemValue,
                                Type = typeof (MenuItem)
                            });

                            _menuItems.Add(new MenuItem(data.ItemName, data.MenuItemName, data.ItemType, data.TextHeight));

                            _underlines.Add(new SeparatorData
                            {
                                Color = menu[data.MenuItemName] ? EnabledUnderlineColor : DisabledUnderlineColor,
                                Positions =
                                    new[]
                                    {
                                        new Vector2(
                                            (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f),
                                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) +
                                            data.TextHeight),
                                        new Vector2(
                                            (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) +
                                            itemValue.GetTextRectangle().Width,
                                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) +
                                            data.TextHeight)
                                    },
                                Width = 1
                            });

                        }
                        else if (data.ItemType == ItemTypes.Integer)
                        {
                            itemName = new Text(data.ItemName, data.TextHeight, (int) Position.X + DefaultSpacing,
                                (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10), TextColor);
                            itemValue = new Text("Value : " + menu[data.MenuItemName, true],
                                data.TextHeight,
                                (int) (Position.X + itemName.GetTextRectangle().Width + DefaultSpacing*2.5f),
                                (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10),
                                TextColor);

                            _permaShowItems.Add(new ItemData
                            {
                                ItemType = ItemTypes.Integer,
                                ItemName = itemName,
                                ItemValue = itemValue,
                                Type = typeof (MenuItem)
                            });

                            _menuItems.Add(new MenuItem(data.ItemName, data.MenuItemName, data.ItemType, data.TextHeight));
                        }

                        _separators.Add(new SeparatorData
                        {
                            Color = SeparatorColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(Position.X,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5),
                                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5)
                                },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(this, typeof(MenuItem));
                    }
                } else if (value.GetType() == typeof (BoolItemData))
                {
                    var data = value as BoolItemData;

                    if (!string.IsNullOrEmpty(data?.ItemName) && data.TextHeight > 1)
                    {

                        var lastItem = _permaShowItems.Last();

                        var itemName = new Text(data.ItemName, data.TextHeight, (int) Position.X + DefaultSpacing,
                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10), TextColor);
                        var itemValue = new Text(data.Value ? "[ ✓ ] Enabled" : "[ X ] Disabled",
                            data.TextHeight,
                            (int) (Position.X + itemName.GetTextRectangle().Width + DefaultSpacing*2.5f),
                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10),
                            TextColor);

                        _permaShowItems.Add(new ItemData
                        {
                            ItemType = ItemTypes.Bool,
                            ItemName = itemName,
                            ItemValue = itemValue,
                            Type = typeof (BoolItemData)
                        });

                        _underlines.Add(new SeparatorData
                        {
                            Color = data.Value ? EnabledUnderlineColor : DisabledUnderlineColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(
                                        (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f),
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight),
                                    new Vector2(
                                        (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) +
                                        itemValue.GetTextRectangle().Width,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight)
                                },
                            Width = 1
                        });

                        _separators.Add(new SeparatorData
                        {
                            Positions =
                                new[]
                                {
                                    new Vector2(Position.X,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5),
                                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5)
                                },
                            Width = 2
                        });

                        var t = new BoolItemData(data.ItemName, data.Value, data.TextHeight);
                        t.OnValueChangeEvent += BoolItemData_OnValueChangeEvent;

                        return (T)Convert.ChangeType(t, typeof(BoolItemData));
                    }
                }
                else if (value.GetType() == typeof (StringItemData))
                {
                    var data = value as StringItemData;

                    if (!string.IsNullOrEmpty(data?.ItemName) && data.TextHeight > 1)
                    {
                        var lastItem = _permaShowItems.Last();

                        var itemName = new Text(data.ItemName, data.TextHeight, (int) Position.X + DefaultSpacing,
                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10), TextColor);
                        var itemValue = new Text(data.Value,
                            data.TextHeight,
                            (int) (Position.X + itemName.GetTextRectangle().Width + DefaultSpacing*2.5f),
                            (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10),
                            TextColor);

                        _permaShowItems.Add(new ItemData
                        {
                            ItemType = ItemTypes.String,
                            ItemName = itemName,
                            ItemValue = itemValue,
                            Type = typeof (StringItemData)
                        });

                        _separators.Add(new SeparatorData
                        {
                            Color = SeparatorColor,
                            Positions =
                                new[]
                                {
                                    new Vector2(Position.X,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5),
                                    new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing,
                                        (int) (lastItem.ItemName.Y + lastItem.ItemName.Height + 10) + data.TextHeight +
                                        5)
                                },
                            Width = 2
                        });

                        var t = new StringItemData(data.ItemName, data.Value, data.TextHeight);
                        t.OnValueChangeEvent += StringItemData_OnValueChangeEvent;
                        return (T)Convert.ChangeType(t, typeof(StringItemData));
                    }
                }
            }
            UpdatePositions();
            return  (T)(object)null;
        }

        private void StringItemData_OnValueChangeEvent(object sender, StringChangeValueEventArgs args)
        {
            var item = _permaShowItems.FirstOrDefault(e => e.ItemName.Message == args.ItemName);

            if (item == null)
                return;

            var index = _permaShowItems.FindIndex(data => item.ItemName == data.ItemName);

            _permaShowItems[index].ItemValue.Message = args.Value;
        }

        private void BoolItemData_OnValueChangeEvent(object sender, BoolChangeValueEventArgs args)
        {
            var item = _permaShowItems.FirstOrDefault(e => e.ItemName.Message == args.ItemName);

            if (item == null)
                return;

            var index = _permaShowItems.Where(x => x.ItemType == ItemTypes.Bool).ToList().FindIndex(data => item.ItemName == data.ItemName);

            if (args.Value && item.ItemValue.Message.Contains("Disabled"))
            {
                var textWidth = item.ItemValue.GetTextRectangle().Width;
                item.ItemValue.Message = "[ ✓ ] Enabled";
                _underlines.ToArray()[index].Color = new ColorBGRA(173, 255, 47, 255);
                _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                              textWidth +
                                                              item.ItemValue.GetTextRectangle().Width;
            }
            else if (!args.Value && item.ItemValue.Message.Contains("Enabled"))
            {
                var textWidth = item.ItemValue.GetTextRectangle().Width;
                item.ItemValue.Message = "[ X ] Disabled";
                _underlines.ToArray()[index].Color = new ColorBGRA(255, 0, 0, 255);
                _underlines.ToArray()[index].Positions[1].X = _underlines.ToArray()[index].Positions[1].X -
                                                              textWidth +
                                                              item.ItemValue.GetTextRectangle().Width;
            }
        }

        private bool IsPositionOnPermaShow(Vector2 position)
        {
            if (!Enabled)
                return false;

            var lastSeparator = _separators.Last();

            return position.X >= Position.X && position.X <= Position.X + GetMaxTextLength() + DefaultSpacing * 2 && position.Y >= Position.Y && position.Y <= lastSeparator.Positions[0].Y + 5;
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
        }
    }

    public enum ItemTypes
    {
        Bool,
        Integer,
        String
    }

    internal interface IPermaShowItem
    {

    }

    internal class MenuItem : IPermaShowItem
    {
        public string ItemName { get; set; }
        public string MenuItemName { get; set; }
        public ItemTypes ItemType { get; set; }
        public uint TextHeight { get; set; }

        public MenuItem(string itemName, string menuItemName, ItemTypes itemType, uint textHeight)
        {
            ItemName = itemName;
            MenuItemName = menuItemName;
            ItemType = itemType;
            TextHeight = textHeight;
        }
    }

    internal class BoolItemData : IPermaShowItem
    {
        public string ItemName { get; set; }

        private bool _value;
        public bool Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    OnValueChangeEvent?.Invoke(this, new BoolChangeValueEventArgs { ItemName = ItemName, Value = value});
                }
                _value = value;
            }
        }

        public uint TextHeight { get; set; }

        public delegate void OnValueChange(object sender, BoolChangeValueEventArgs args);
        public event OnValueChange OnValueChangeEvent;

        public BoolItemData(string itemName, bool value, uint textHeight)
        {
            ItemName = itemName;
            Value = value;
            TextHeight = textHeight;
        }
    }


    internal class BoolChangeValueEventArgs : EventArgs
    {
        public string ItemName { get; set; }
        public bool Value { get; set; }
    }

    internal class StringChangeValueEventArgs : EventArgs
    {
        public string ItemName { get; set; }
        public string Value { get; set; }
    }

    internal class StringItemData: IPermaShowItem
    {
        public string ItemName { get; set; }
        private string _value;

        public string Value { get { return _value; }
            set
            {
                if (_value != value)
                {
                    OnValueChangeEvent?.Invoke(this, new StringChangeValueEventArgs { ItemName = ItemName, Value = value });
                }
                _value = value;
            }
        }
        public uint TextHeight { get; set; }

        public delegate void OnValueChange(object sender, StringChangeValueEventArgs args);
        public event OnValueChange OnValueChangeEvent;

        public StringItemData(string itemName, string value, uint textHeight)
        {
            ItemName = itemName;
            Value = value;
            TextHeight = textHeight;
        }
    }

    internal class ItemData
    {
        public Text ItemName { get; set; }
        public Text ItemValue { get; set; }
        public ItemTypes ItemType { get; set; }
        public Type Type { get; set; }
    }

    internal class SeparatorData
    {
        public ColorBGRA Color { get; set; }
        public uint Width;
        public Vector2[] Positions { get; set; }
    }
}