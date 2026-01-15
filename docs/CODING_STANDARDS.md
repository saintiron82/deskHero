# 📜 Development Guidelines & Coding Standards

## 1. Object-Oriented Principles (OOP)

### 1.1 Single Responsibility Principle (SRP)
- **Rule**: Each class should have only **one reason to change**.
- **Guideline**: Do not mix UI logic, Business logic, and Data access in a single class.
    - *Bad*: `MainWindow.xaml.cs` handling DB connections, Game Logic, and Animations.
    - *Good*: `MainWindow` controls UI, `GameManager` handles logic, `Repository` handles data.

### 1.2 Encapsulation
- Keep fields `private` or `protected`.
- Use Properties for public access.
- Expose only what is necessary (Minimize API surface).

## 2. File Size Limits

### 2.1 Maximum File Size
To maintain readability and manageability, strict file size limits are enforced.

- **Soft Limit**: **300 lines**. (Warning zone, consider refactoring)
- **Hard Limit**: **500 lines**. (Action required)
- **Critical Limit**: **1000 lines**. (Refactoring **MANDATORY** before any new feature)

### 2.2 Handling Large Files
If a file exceeds the limit:
1.  **Identify Responsibilities**: Group methods and fields by function.
2.  **Extract Classes**: Create new classes for these groups (e.g., `CombatController`, `AnimationHelper`).
3.  **Delegate**: The original class should instance the new classes and delegate calls.

## 3. C# Coding Conventions

- **Classes/Methods/Properties**: PascalCase (`MainWindow`, `StartGame`, `PlayerHealth`)
- **Private Fields**: _camelCase (`_playerHealth`, `_gameManager`)
- **Local Variables**: camelCase (`damage`, `newItem`)
- **Constants**: UPPER_CASE or PascalCase (`MAX_HEALTH`, `DefaultSpeed`)

## 4. UI vs Logic Separation (MVVM)

- **View (XAML + xaml.cs)**: Only handles UI rendering and user input events.
- **ViewModel**: Handles presentation logic and state.
- **Model**: Represents data and business rules.
- **Controller/Manager**: Orchestrates complex logic flows not suitable for pure MVVM (e.g., Global Input Hooks).
