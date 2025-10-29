# Claude Code Guidelines for This Project

## Core Principles

### 1. Always Consider Best Practices First
Before implementing any solution:
- **STOP** and consider if there's a cleaner, more standard approach
- Ask yourself: "Is this the way the framework/engine intends this to be done?"
- Check if there's a design-time solution instead of a runtime workaround

### 2. Seek Approval for Brute Force Solutions
If you identify that your solution is:
- A workaround rather than a proper fix
- Programmatically fixing something that could be fixed in the editor/design
- Adding runtime code to compensate for incorrect setup
- A "band-aid" solution

**You MUST:**
1. Explain the brute force approach
2. Explain the proper/best practice approach
3. Ask the user which they prefer before implementing

### 3. Unity-Specific Best Practices

#### Design-Time vs Runtime
- **Prefer design-time solutions** in the Unity Editor when possible
- Use the Inspector to configure, disable, or hide placeholder objects
- Reserve runtime code for actual game logic, not fixing design mistakes

#### Placeholder Objects
- Placeholders should be **disabled in the Inspector** before testing/building
- Do NOT destroy placeholders at runtime - fix them in the editor
- Use prefabs for runtime instantiation, not scene placeholders

#### Performance
- Avoid unnecessary `foreach` loops over children to destroy objects at Start
- Don't add runtime overhead for problems solvable in the editor

## Example: The Placeholder Problem

### ❌ Wrong (Brute Force)
```csharp
void Start() {
    ClearPlayerIconPlaceholders(); // Destroying at runtime
}
```

### ✅ Right (Best Practice)
- Select placeholder GameObject in Unity hierarchy
- Uncheck the checkbox at top of Inspector to disable it
- No runtime code needed

## Communication Standards

### When Proposing Solutions
Always structure your response as:
```
**Quick Fix (Brute Force):**
[Explain the workaround approach]

**Proper Solution (Best Practice):**
[Explain the correct approach]

**Recommendation:**
[State which you recommend and why]

**Question:**
Which approach would you like me to implement?
```

### Never Assume
- Don't assume the user wants the fastest solution
- Don't assume the user won't want to manually fix something in Unity
- Don't assume code is always better than editor configuration

## Project-Specific Notes

### Unity Editor Workflow
- This is a Unity game project with both Desktop and Mobile displays
- Many UI elements are designed visually in the Unity Editor
- Scene hierarchy often contains design-time placeholders for layout purposes
- Always check if something can be solved in the Inspector before writing code

### When in Doubt
Ask: "Would you like me to [implement this] or would you prefer to [manually fix this in Unity]?"

---

**Remember:** Clean, maintainable code that follows framework conventions is ALWAYS better than clever workarounds that save 5 minutes.
