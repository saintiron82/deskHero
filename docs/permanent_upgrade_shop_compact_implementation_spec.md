# 영구 업그레이드 상점 컴팩트 UI 구현 명세서
**작성일**: 2026-01-16
**담당**: lily (구현 담당자)
**작성자**: jina (게임 디자인 AI)

---

## 1. 개요

### 1.1 작업 목표
기존의 복잡한 검색/필터/정렬 UI를 제거하고, 심플하고 직관적인 탭 기반 컴팩트 그리드 레이아웃으로 전면 리뉴얼합니다.

### 1.2 변경 범위
- **XAML**: PermanentUpgradeShop.xaml
- **ViewModel**: ViewModels/PermanentUpgradeShopViewModel.cs
- **CodeBehind**: PermanentUpgradeShop.xaml.cs

### 1.3 핵심 변경사항
- 검색/필터/정렬 UI 완전 제거
- 4개 탭만 유지 (⚔️ 기본 능력, 💰 재화 보너스, ⚙️ 유틸리티, 🚀 시작 보너스)
- 3열 컴팩트 그리드 레이아웃 (180x140px 카드)
- 탭에 구매 가능 개수 뱃지 추가 (선택 사항)
- 툴팁으로 상세 정보 표시
- 카드 컴팩트화 (아이콘, 이름, 레벨, 현재 효과만 표시)

---

## 2. UI 레이아웃 상세 명세

### 2.1 전체 구조
```
┌─────────────────────────────────────────────────────┐
│  HEADER (Auto Height) - 타이틀 & 통화 정보          │ Row 0
├─────────────────────────────────────────────────────┤
│  TABS (Auto Height) - 카테고리 탭 (4개)             │ Row 1
├─────────────────────────────────────────────────────┤
│  CONTENT (Height=*) - 컴팩트 카드 그리드             │ Row 2
│  3열 레이아웃, ScrollViewer                         │
├─────────────────────────────────────────────────────┤
│  FOOTER (Auto Height) - 힌트 & 닫기 버튼            │ Row 3
└─────────────────────────────────────────────────────┘
```

### 2.2 윈도우 크기
- **Width**: 900px (1100px → 900px 축소)
- **Height**: 700px (750px → 700px 축소)
- **WindowStyle**: None
- **AllowsTransparency**: True
- **Background**: Transparent

---

## 3. 컴포넌트별 상세 명세

### 3.1 헤더 (Grid Row 0)
**변경 없음** - 현재 구조 유지

#### XAML 코드
```xaml
<Border Grid.Row="0" Background="#161B22" CornerRadius="15,15,0,0" Padding="25,20">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <!-- Left: Title -->
        <StackPanel Grid.Column="0" VerticalAlignment="Center">
            <TextBlock Text="💎 영구 업그레이드 상점" FontSize="24" FontWeight="Bold" Foreground="White">
                <TextBlock.Effect>
                    <DropShadowEffect Color="#00CED1" BlurRadius="15" ShadowDepth="0" Opacity="0.6"/>
                </TextBlock.Effect>
            </TextBlock>
            <TextBlock Text="크리스탈을 사용하여 영구적인 능력을 강화하세요"
                       FontSize="11" Foreground="#8B949E" Margin="0,4,0,0"/>
        </StackPanel>

        <!-- Right: Currency Panel -->
        <Border Grid.Column="1" Background="#0D1117" CornerRadius="10" Padding="20,12" BorderBrush="#30363D" BorderThickness="1">
            <StackPanel>
                <!-- Current Crystals -->
                <Border Background="#1E40AF" CornerRadius="8" Padding="15,8" Margin="0,0,0,8">
                    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                        <TextBlock Text="💎" FontSize="24" VerticalAlignment="Center" Margin="0,0,10,0"/>
                        <StackPanel VerticalAlignment="Center">
                            <TextBlock Text="보유 크리스탈" FontSize="9" Foreground="#93C5FD"/>
                            <TextBlock x:Name="CurrentCrystalsText" Text="0" FontSize="20" FontWeight="Bold" Foreground="White"/>
                        </StackPanel>
                    </StackPanel>
                </Border>

                <!-- Lifetime Stats -->
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="10"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>

                    <!-- Total Earned -->
                    <StackPanel Grid.Column="0">
                        <TextBlock Text="총 획득" FontSize="8" Foreground="#6E7681" HorizontalAlignment="Center"/>
                        <TextBlock x:Name="LifetimeEarnedText" Text="0" FontSize="11" FontWeight="SemiBold" Foreground="#FFD700" HorizontalAlignment="Center"/>
                    </StackPanel>

                    <!-- Separator -->
                    <Border Grid.Column="1" Width="1" Background="#30363D"/>

                    <!-- Total Spent -->
                    <StackPanel Grid.Column="2">
                        <TextBlock Text="총 사용" FontSize="8" Foreground="#6E7681" HorizontalAlignment="Center"/>
                        <TextBlock x:Name="LifetimeSpentText" Text="0" FontSize="11" FontWeight="SemiBold" Foreground="#8B949E" HorizontalAlignment="Center"/>
                    </StackPanel>
                </Grid>
            </StackPanel>
        </Border>
    </Grid>
</Border>
```

---

### 3.2 탭 (Grid Row 1)

#### 3.2.1 레이아웃
- **배경색**: #21262D
- **Padding**: 25,10,25,0
- **탭 간격**: 5px

#### 3.2.2 탭 버튼 스타일
**기존 TabButton 스타일 유지**, 단 Content에 뱃지 추가

#### 3.2.3 뱃지 표시 로직 (선택 사항)
탭 Content 형식:
```
⚔️ 기본 능력 (3)
```
- 숫자는 구매 가능한 업그레이드 개수 (CanAfford = true)
- 개수가 0이면 뱃지 숨김

#### 3.2.4 XAML 코드
```xaml
<Border Grid.Row="1" Background="#21262D" Padding="25,10,25,0">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
        <RadioButton x:Name="TabBaseStats" Style="{StaticResource TabButton}" Margin="0,0,5,0"
                     IsChecked="True" Checked="Tab_Checked">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="⚔️ 기본 능력"/>
                <TextBlock x:Name="BadgeBaseStats" Text="" FontSize="10" FontWeight="Bold"
                           Foreground="#10B981" Margin="5,0,0,0"/>
            </StackPanel>
        </RadioButton>

        <RadioButton x:Name="TabCurrencyBonus" Style="{StaticResource TabButton}" Margin="0,0,5,0"
                     Checked="Tab_Checked">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="💰 재화 보너스"/>
                <TextBlock x:Name="BadgeCurrencyBonus" Text="" FontSize="10" FontWeight="Bold"
                           Foreground="#10B981" Margin="5,0,0,0"/>
            </StackPanel>
        </RadioButton>

        <RadioButton x:Name="TabUtility" Style="{StaticResource TabButton}" Margin="0,0,5,0"
                     Checked="Tab_Checked">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="⚙️ 유틸리티"/>
                <TextBlock x:Name="BadgeUtility" Text="" FontSize="10" FontWeight="Bold"
                           Foreground="#10B981" Margin="5,0,0,0"/>
            </StackPanel>
        </RadioButton>

        <RadioButton x:Name="TabStartingBonus" Style="{StaticResource TabButton}"
                     Checked="Tab_Checked">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="🚀 시작 보너스"/>
                <TextBlock x:Name="BadgeStartingBonus" Text="" FontSize="10" FontWeight="Bold"
                           Foreground="#10B981" Margin="5,0,0,0"/>
            </StackPanel>
        </RadioButton>
    </StackPanel>
</Border>
```

---

### 3.3 컴팩트 카드 (Grid Row 2)

#### 3.3.1 카드 크기
- **Width**: 180px (고정)
- **Height**: 140px (고정)
- **Margin**: 8px (카드 간격)

#### 3.3.2 카드 구조
```
┌──────────────────────────┐
│ 🎯 [이름]        [+25%]  │  <- 헤더 (아이콘, 이름, 현재 효과)
│ Lv.5/30                  │  <- 레벨 표시
├──────────────────────────┤
│ [상세 설명 1줄]          │  <- 짧은 설명 (1줄)
├──────────────────────────┤
│ 다음: +28%               │  <- 다음 레벨 효과
├──────────────────────────┤
│ [💎 250]                 │  <- 구매 버튼
└──────────────────────────┘
```

#### 3.3.3 컴팩트 카드 DataTemplate
**완전히 새로 작성**

```xaml
<!-- 컴팩트 업그레이드 카드 스타일 -->
<Style x:Key="CompactUpgradeCard" TargetType="Border">
    <Setter Property="Background" Value="#1E1E1E"/>
    <Setter Property="BorderBrush" Value="#333"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Width" Value="180"/>
    <Setter Property="Height" Value="140"/>
    <Setter Property="Margin" Value="8"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#252525"/>
            <Setter Property="BorderBrush" Value="#00CED1"/>
        </Trigger>
    </Style.Triggers>
</Style>
```

#### 3.3.4 카드 내부 구조 (C# CodeBehind에서 생성)
**CreateCompactUpgradeCard** 메서드로 생성

```csharp
private Border CreateCompactUpgradeCard(UpgradeCardViewModel upgrade)
{
    var card = new Border
    {
        Style = (Style)FindResource("CompactUpgradeCard"),
        Tag = upgrade.Id,
        Padding = new Thickness(10)
    };

    // 툴팁 추가
    var tooltip = new ToolTip
    {
        Content = CreateTooltipContent(upgrade),
        Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 206, 209)),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(12)
    };
    card.ToolTip = tooltip;

    var mainStack = new StackPanel();

    // === 헤더: 아이콘 + 이름 + 현재 효과 ===
    var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };

    var leftStack = new StackPanel { Orientation = Orientation.Horizontal };

    var icon = new TextBlock
    {
        Text = upgrade.Icon,
        FontSize = 16,
        Margin = new Thickness(0, 0, 5, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    var nameText = new TextBlock
    {
        Text = upgrade.ShortName, // 축약 이름 사용
        FontSize = 11,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.White,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis,
        MaxWidth = 80
    };

    leftStack.Children.Add(icon);
    leftStack.Children.Add(nameText);

    var currentEffect = new TextBlock
    {
        Text = upgrade.CurrentEffect,
        FontSize = 13,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center
    };

    headerGrid.Children.Add(leftStack);
    headerGrid.Children.Add(currentEffect);
    mainStack.Children.Add(headerGrid);

    // === 레벨 표시 ===
    var levelText = new TextBlock
    {
        Text = upgrade.LevelDisplay,
        FontSize = 9,
        Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129)),
        Margin = new Thickness(0, 0, 0, 8)
    };
    mainStack.Children.Add(levelText);

    // === 구분선 ===
    var separator1 = new Border
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
        Margin = new Thickness(0, 0, 0, 6)
    };
    mainStack.Children.Add(separator1);

    // === 다음 레벨 효과 ===
    var nextLevelStack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

    var nextLevelLabel = new TextBlock
    {
        Text = upgrade.IsMaxed ? "상태" : "다음",
        FontSize = 8,
        Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129))
    };

    var nextLevelValue = new TextBlock
    {
        Text = upgrade.IsMaxed ? "MAX" : upgrade.NextLevelEffect,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = upgrade.IsMaxed
            ? new SolidColorBrush(Color.FromRgb(96, 165, 250))
            : new SolidColorBrush(Color.FromRgb(16, 185, 129))
    };

    nextLevelStack.Children.Add(nextLevelLabel);
    nextLevelStack.Children.Add(nextLevelValue);
    mainStack.Children.Add(nextLevelStack);

    // === 구분선 ===
    var separator2 = new Border
    {
        Height = 1,
        Background = new SolidColorBrush(Color.FromRgb(48, 54, 61)),
        Margin = new Thickness(0, 0, 0, 6)
    };
    mainStack.Children.Add(separator2);

    // === 구매 버튼 ===
    var button = new Button
    {
        Height = 28,
        Tag = upgrade.Id,
        FontSize = 10,
        FontWeight = FontWeights.SemiBold
    };
    button.Click += BuyUpgrade_Click;

    var buttonText = new TextBlock
    {
        Text = upgrade.ButtonText,
        FontSize = 10
    };
    button.Content = buttonText;

    // 버튼 스타일 설정
    if (upgrade.IsMaxed)
    {
        button.Background = new SolidColorBrush(Color.FromRgb(30, 64, 175));
        button.Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250));
        button.IsEnabled = false;
    }
    else if (upgrade.CanAfford)
    {
        button.Background = new SolidColorBrush(Color.FromRgb(5, 150, 105));
        button.Foreground = Brushes.White;
        button.IsEnabled = true;
    }
    else
    {
        button.Background = new SolidColorBrush(Color.FromRgb(75, 85, 99));
        button.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        button.IsEnabled = false;
    }

    button.Template = new ControlTemplate(typeof(Button));
    var factory = new FrameworkElementFactory(typeof(Border));
    factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
    factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
    factory.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
    var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
    contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
    contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
    factory.AppendChild(contentPresenter);
    button.Template.VisualTree = factory;

    mainStack.Children.Add(button);

    card.Child = mainStack;
    return card;
}
```

#### 3.3.5 툴팁 구조
```csharp
private StackPanel CreateTooltipContent(UpgradeCardViewModel upgrade)
{
    var tooltipStack = new StackPanel { Width = 250 };

    // 타이틀
    var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
    var titleIcon = new TextBlock
    {
        Text = upgrade.Icon,
        FontSize = 20,
        Margin = new Thickness(0, 0, 8, 0)
    };
    var titleText = new TextBlock
    {
        Text = upgrade.Name,
        FontSize = 14,
        FontWeight = FontWeights.Bold,
        Foreground = Brushes.White
    };
    titleStack.Children.Add(titleIcon);
    titleStack.Children.Add(titleText);
    tooltipStack.Children.Add(titleStack);

    // 레벨 & 현재 효과
    var levelGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
    levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

    var levelText = new TextBlock
    {
        Text = upgrade.LevelDisplay,
        FontSize = 10,
        Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
    };
    Grid.SetColumn(levelText, 0);

    var currentEffectText = new TextBlock
    {
        Text = $"현재: {upgrade.CurrentEffect}",
        FontSize = 11,
        FontWeight = FontWeights.Bold,
        Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
        HorizontalAlignment = HorizontalAlignment.Right
    };
    Grid.SetColumn(currentEffectText, 1);

    levelGrid.Children.Add(levelText);
    levelGrid.Children.Add(currentEffectText);
    tooltipStack.Children.Add(levelGrid);

    // 설명
    var descText = new TextBlock
    {
        Text = upgrade.Description,
        FontSize = 11,
        Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 0, 0, 10)
    };
    tooltipStack.Children.Add(descText);

    // 다음 레벨 정보
    if (!upgrade.IsMaxed)
    {
        var nextBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var nextStack = new StackPanel();
        var nextLabel = new TextBlock
        {
            Text = "다음 레벨 효과",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(110, 118, 129))
        };
        var nextValue = new TextBlock
        {
            Text = upgrade.NextLevelEffect,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
        };

        nextStack.Children.Add(nextLabel);
        nextStack.Children.Add(nextValue);
        nextBorder.Child = nextStack;
        tooltipStack.Children.Add(nextBorder);

        // 비용
        var costText = new TextBlock
        {
            Text = $"비용: 💎 {upgrade.Cost:N0}",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = upgrade.CanAfford
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        tooltipStack.Children.Add(costText);
    }
    else
    {
        var maxText = new TextBlock
        {
            Text = "최대 레벨 달성",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        tooltipStack.Children.Add(maxText);
    }

    return tooltipStack;
}
```

#### 3.3.6 3열 그리드 레이아웃
**LoadCategoryUpgrades** 메서드 수정

```csharp
private void LoadCategoryUpgrades(string category)
{
    _currentCategory = category;
    UpgradeGrid.Children.Clear();
    UpgradeGrid.ColumnDefinitions.Clear();
    UpgradeGrid.RowDefinitions.Clear();

    // 해당 카테고리의 업그레이드 필터링
    var categoryUpgrades = _viewModel.AllUpgrades
        .Where(u => u.CategoryKey == category)
        .ToList();

    if (categoryUpgrades.Count == 0)
    {
        // 업그레이드가 없는 경우 메시지 표시
        var message = new TextBlock
        {
            Text = "이 카테고리에는 업그레이드가 없습니다.",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 50, 0, 0)
        };
        UpgradeGrid.Children.Add(message);
        UpdateBadges(); // 뱃지 업데이트
        return;
    }

    // 3열 그리드 구성
    int columns = 3;
    for (int i = 0; i < columns; i++)
    {
        UpgradeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }

    // 카드 배치
    for (int i = 0; i < categoryUpgrades.Count; i++)
    {
        var upgrade = categoryUpgrades[i];
        int col = i % columns;
        int row = i / columns;

        // 행 추가 (필요한 경우)
        while (UpgradeGrid.RowDefinitions.Count <= row)
        {
            UpgradeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // 컴팩트 카드 생성
        var card = CreateCompactUpgradeCard(upgrade);
        Grid.SetColumn(card, col);
        Grid.SetRow(card, row);
        UpgradeGrid.Children.Add(card);
    }

    UpdateBadges(); // 뱃지 업데이트
}
```

---

### 3.4 푸터 (Grid Row 3)
**변경 없음** - 현재 구조 유지

---

## 4. ViewModel 변경 사항

### 4.1 UpgradeCardViewModel에 ShortName 추가
컴팩트 카드용 축약 이름

```csharp
public class UpgradeCardViewModel : ViewModelBase
{
    private bool _canAfford;
    private bool _isMaxed;

    public string Id { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = ""; // ← 추가
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryKey { get; set; } = "";
    public int CurrentLevel { get; set; }
    public int MaxLevel { get; set; }
    public double IncrementPerLevel { get; set; }
    public string CurrentEffect { get; set; } = "";
    public string NextLevelEffect { get; set; } = "";
    public int Cost { get; set; }

    public bool CanAfford
    {
        get => _canAfford;
        set => SetProperty(ref _canAfford, value);
    }

    public bool IsMaxed
    {
        get => _isMaxed;
        set => SetProperty(ref _isMaxed, value);
    }

    public string LevelDisplay
    {
        get
        {
            if (MaxLevel > 0)
                return $"Lv.{CurrentLevel}/{MaxLevel}";
            return $"Lv.{CurrentLevel}";
        }
    }

    public string ButtonText
    {
        get
        {
            if (IsMaxed)
                return "MAX";
            return $"💎 {Cost:N0}";
        }
    }
}
```

### 4.2 LoadData 메서드 수정
ShortName 생성 로직 추가

```csharp
public void LoadData()
{
    var currency = _saveManager.CurrentSave.PermanentCurrency;
    CurrentCrystals = currency.Crystals;
    LifetimeEarned = currency.LifetimeCrystalsEarned;
    LifetimeSpent = currency.LifetimeCrystalsSpent;

    AllUpgrades.Clear();

    var definitions = _progressionManager.GetAllUpgradeDefinitions();
    foreach (var def in definitions)
    {
        var progress = _saveManager.CurrentSave.PermanentUpgrades.FirstOrDefault(p => p.Id == def.Id);
        int currentLevel = progress?.CurrentLevel ?? 0;

        string fullName = def.Localization.ContainsKey("ko-KR") ? def.Localization["ko-KR"].Name : def.Id;

        var card = new UpgradeCardViewModel
        {
            Id = def.Id,
            Icon = def.Icon,
            Name = fullName,
            ShortName = GenerateShortName(fullName), // ← 추가
            Description = def.Localization.ContainsKey("ko-KR") ? def.Localization["ko-KR"].Description : "",
            Category = GetCategoryDisplayName(def.Category),
            CategoryKey = def.Category,
            CurrentLevel = currentLevel,
            MaxLevel = def.MaxLevel,
            IncrementPerLevel = def.IncrementPerLevel,
            IsMaxed = def.MaxLevel > 0 && currentLevel >= def.MaxLevel
        };

        // 현재 효과 계산
        card.CurrentEffect = FormatEffect(def, currentLevel);

        // 다음 레벨 효과 계산
        if (!card.IsMaxed)
        {
            card.NextLevelEffect = FormatEffect(def, currentLevel + 1);
            card.Cost = _progressionManager.CalculateUpgradeCost(def, currentLevel);
            card.CanAfford = CurrentCrystals >= card.Cost;
        }

        AllUpgrades.Add(card);
    }
}

/// <summary>
/// 짧은 이름 생성 (컴팩트 카드용)
/// </summary>
private string GenerateShortName(string fullName)
{
    // "기본 공격력" → "공격력"
    // "영구 골드+" → "골드+"
    // "시작 레벨" → "시작Lv"

    var shortNameMap = new Dictionary<string, string>
    {
        { "기본 공격력", "공격력" },
        { "공격력 배수", "공격*" },
        { "크리티컬 확률", "크리확률" },
        { "크리티컬 배율", "크리배율" },
        { "멀티히트 확률", "멀티히트" },
        { "영구 골드+", "골드+" },
        { "영구 골드*", "골드*" },
        { "크리스탈+", "크리+" },
        { "크리스탈*", "크리*" },
        { "기본 시간 연장", "시간↑" },
        { "업그레이드 할인", "할인" },
        { "시작 레벨", "시작Lv" },
        { "시작 골드", "시작G" },
        { "시작 키보드", "시작⌨️" },
        { "시작 마우스", "시작🖱️" },
        { "시작 골드+", "시작G+" },
        { "시작 골드*", "시작G*" },
        { "시작 콤보유연성", "시작콤보" },
        { "시작 콤보데미지", "시작콤보D" }
    };

    return shortNameMap.ContainsKey(fullName) ? shortNameMap[fullName] : fullName;
}
```

### 4.3 제거할 항목
없음 (검색/필터 기능이 ViewModel에 없었음)

---

## 5. CodeBehind 변경 사항

### 5.1 추가할 메서드

#### 5.1.1 UpdateBadges (뱃지 업데이트)
```csharp
/// <summary>
/// 탭 뱃지 업데이트 (구매 가능 개수 표시)
/// </summary>
private void UpdateBadges()
{
    var categories = new[]
    {
        new { Key = "base_stats", Badge = BadgeBaseStats },
        new { Key = "currency_bonus", Badge = BadgeCurrencyBonus },
        new { Key = "utility", Badge = BadgeUtility },
        new { Key = "starting_bonus", Badge = BadgeStartingBonus }
    };

    foreach (var cat in categories)
    {
        int affordableCount = _viewModel.AllUpgrades
            .Count(u => u.CategoryKey == cat.Key && u.CanAfford && !u.IsMaxed);

        if (affordableCount > 0)
        {
            cat.Badge.Text = $"({affordableCount})";
            cat.Badge.Visibility = Visibility.Visible;
        }
        else
        {
            cat.Badge.Text = "";
            cat.Badge.Visibility = Visibility.Collapsed;
        }
    }
}
```

#### 5.1.2 CreateCompactUpgradeCard
위의 3.3.4 참조

#### 5.1.3 CreateTooltipContent
위의 3.3.5 참조

### 5.2 수정할 메서드

#### 5.2.1 RefreshUI
```csharp
private void RefreshUI()
{
    _viewModel.LoadData();

    // 헤더 통화 정보 업데이트
    CurrentCrystalsText.Text = _viewModel.CurrentCrystals.ToString("N0");
    LifetimeEarnedText.Text = _viewModel.LifetimeEarned.ToString("N0");
    LifetimeSpentText.Text = _viewModel.LifetimeSpent.ToString("N0");

    UpdateBadges(); // ← 추가
}
```

#### 5.2.2 LoadCategoryUpgrades
위의 3.3.6 참조

#### 5.2.3 BuyUpgrade_Click
변경 없음 (현재 구조 유지)

### 5.3 제거할 코드
#### 5.3.1 CreateUpgradeCard 메서드
기존의 CreateUpgradeCard는 삭제하고 CreateCompactUpgradeCard로 대체

---

## 6. 구현 체크리스트

### Phase 1: XAML 수정 (30분)
- [ ] Window Width를 900px로 변경
- [ ] Window Height를 700px로 변경
- [ ] 탭 Content를 StackPanel + TextBlock 구조로 변경 (뱃지용)
- [ ] ScrollViewer Padding 조정 (25,15 → 20,15)
- [ ] CompactUpgradeCard 스타일 추가 (Resources에)
- [ ] 기존 UpgradeCardTemplate 제거 (사용 안 함)

### Phase 2: ViewModel 수정 (20분)
- [ ] UpgradeCardViewModel에 ShortName 프로퍼티 추가
- [ ] GenerateShortName 메서드 추가
- [ ] LoadData 메서드에서 ShortName 생성 로직 추가

### Phase 3: CodeBehind 수정 (60분)
- [ ] CreateCompactUpgradeCard 메서드 구현
- [ ] CreateTooltipContent 메서드 구현
- [ ] LoadCategoryUpgrades 메서드를 3열 그리드로 수정
- [ ] UpdateBadges 메서드 구현
- [ ] RefreshUI에 UpdateBadges 호출 추가
- [ ] 기존 CreateUpgradeCard 메서드 삭제

### Phase 4: 테스트 (30분)
- [ ] 각 탭 전환 테스트 (4개 카테고리)
- [ ] 뱃지 카운트 정확성 검증
- [ ] 카드 호버 효과 확인
- [ ] 툴팁 표시 확인
- [ ] 구매 버튼 클릭 테스트 (구매 가능/불가능/MAX)
- [ ] 구매 후 UI 새로고침 확인
- [ ] 3열 그리드 레이아웃 확인 (카드 정렬, 간격)
- [ ] 스크롤 동작 확인

### Phase 5: 디버깅 및 최적화 (20분)
- [ ] 메모리 누수 확인 (카드 생성/삭제 반복)
- [ ] 툴팁 생성 성능 확인
- [ ] 애니메이션 부드러움 확인

---

## 7. 주의사항

### 7.1 기존 기능 유지
- **구매 로직**: PermanentProgressionManager 호출 방식 변경 없음
- **저장 로직**: SaveManager 호출 방식 변경 없음
- **통화 업데이트**: RefreshUI 호출 타이밍 유지
- **ESC 키 닫기**: Window_KeyDown 이벤트 유지
- **구매 애니메이션**: PlayPurchaseAnimation, PlayErrorAnimation 유지

### 7.2 성능 고려사항
- **카드 생성 최적화**: LoadCategoryUpgrades가 탭 전환 시마다 호출되므로, 카드 생성을 최적화해야 함
- **툴팁 지연 생성**: 툴팁은 필요 시 생성 (카드 생성 시 한 번만)
- **이벤트 핸들러 누수 방지**: BuyUpgrade_Click 이벤트 핸들러가 중복 등록되지 않도록 주의

### 7.3 에러 처리
- **카테고리 없음**: categoryUpgrades.Count == 0 처리 로직 유지
- **Null 참조**: upgrade, def, progress null 체크 필수
- **타입 변환 실패**: Tag as string null 체크

### 7.4 UI/UX 세부사항
- **카드 간격**: Margin="8" 고정 (좁은 공간에서 균형 유지)
- **폰트 크기**: 컴팩트 디자인에 맞게 축소 (11pt → 10pt)
- **색상 일관성**: 기존 컬러 팔레트 유지 (#00CED1, #10B981, #58A6FF 등)
- **툴팁 배경**: 기존 카드보다 어둡게 (#1A1A1A)

### 7.5 테스트 시나리오
1. **빈 카테고리 테스트**: 업그레이드가 없는 카테고리 탭 클릭
2. **구매 플로우 테스트**: 크리스탈 부족 → 충분 → 구매 → MAX 도달
3. **뱃지 업데이트 테스트**: 구매 후 뱃지 카운트 감소 확인
4. **다중 탭 전환 테스트**: 탭 A → B → A 반복 (메모리 누수 없는지)
5. **툴팁 표시 테스트**: 모든 카드에서 툴팁 정상 표시 확인

---

## 8. 예상 소요 시간
- **XAML 수정**: 30분
- **ViewModel 수정**: 20분
- **CodeBehind 수정**: 60분
- **테스트**: 30분
- **디버깅**: 20분
- **총 예상 시간**: **2시간 40분**

---

## 9. 완료 후 확인사항
- [ ] 모든 19종 업그레이드가 올바른 카테고리에 표시됨
- [ ] 뱃지 카운트가 실시간으로 업데이트됨
- [ ] 툴팁에 전체 정보가 표시됨
- [ ] 컴팩트 카드가 180x140px로 고정됨
- [ ] 3열 그리드가 정확히 정렬됨
- [ ] 구매 후 UI가 즉시 새로고침됨
- [ ] 검색/필터/정렬 UI가 완전히 제거됨

---

## 10. 추가 개선 제안 (Optional)
- **카드 정렬 옵션**: 비용 순, 레벨 순, 이름 순 (단, UI에 표시하지 않고 내부 로직으로만)
- **즐겨찾기 기능**: 자주 구매하는 업그레이드를 상단에 고정
- **구매 히스토리**: 최근 구매한 업그레이드 표시 (Footer에 작은 로그)
- **카테고리 아이콘 강조**: 탭 선택 시 아이콘 크기 확대 애니메이션
- **구매 효과음**: 성공/실패 시 사운드 피드백

---

**END OF SPECIFICATION**

lily, 이 명세서를 참고하여 구현을 진행해주세요. 질문이 있다면 언제든 요청하세요.
