---
name: flowbite-blazor
description: Use when adding or modifying Flowbite UI components in Blazor apps; covers setup, component selection, icons, JS/CSS requirements, Tailwind alignment, and troubleshooting.
---

# Flowbite Blazor Skill

Use this skill when building or refactoring Blazor UI with Flowbite components.

## Start Here

- Check the repo’s current Flowbite wiring first: `Program.cs`, `wwwroot/index.html`, `_Imports.razor`, `tailwind.config.js`, and the app CSS bundle.
- Prefer Flowbite components over custom Tailwind markup when a matching component exists.
- Use [resources/flowbite-blazor.llms.md](resources/flowbite-blazor.llms.md) for exact component APIs and examples when you need to confirm parameter names or slots.

## Required Setup

- Register Flowbite services in `Program.cs` with `builder.Services.AddFlowbite();`.
- Ensure `index.html` includes Flowbite CSS and JS.
- Add the Floating UI script before `</body>` when using Dropdown, Tooltip, or Popover.
- Keep Tailwind `content` paths and `safelist` entries aligned with the Flowbite components you use.

## Implementation Workflow

1. Pick the Flowbite component that matches the UI need.
2. Import the required namespaces in `_Imports.razor` or in the local component.
3. Use strongly typed parameters, icons, and slots instead of recreating the component with raw HTML.
4. In reusable components, inherit from `FlowbiteComponentBase` when you need Flowbite helper behavior such as class composition.
5. Keep state explicit: use component fields, parameters, and `EventCallback` for parent-child communication.
6. Keep markup accessible and responsive: labels, `sr-only`, `aria-*`, keyboard interaction, and mobile-first layouts.

## Component Selection

- Use `Button`, `Alert`, `Card`, `Badge`, `Avatar`, and `Breadcrumb` for static or low-complexity UI.
- Use `Navbar`, `Sidebar`, `Drawer`, `Dropdown`, `Modal`, `Tooltip`, `Popover`, and `Toast` for interactive shell patterns.
- Use `TextInput`, `Select`, `Checkbox`, `Radio`, `ToggleSwitch`, `RangeSlider`, `FileInput`, and `TextArea` for forms.
- Use `QuickGrid`, `Table`, and `Pagination` for data-heavy views.
- Use `Flowbite.Icons` and `Flowbite.Icons.Extended` for iconography.

## Blazor-Specific Rules

- Keep state in the parent for menus, drawers, and dialogs, then pass the open/selected value down through parameters.
- Avoid JS interop unless a Flowbite feature requires it.
- When a component already provides a callback or slot, prefer that over custom event wiring.
- Reuse existing repo patterns for responsive navbars, sidebars, and dark mode.

## Validation Checklist

- Build the app and confirm Tailwind output includes the classes you used.
- Verify dark mode styling in both themes.
- Confirm dropdowns, tooltips, and popovers position correctly.
- Check that icons render and interactive components still work after refresh or navigation.
- If something looks missing, re-check `_Imports.razor`, the script tags in `index.html`, and Tailwind safelist/content settings.

## Troubleshooting

- Missing styles usually mean a Tailwind `content` or `safelist` mismatch, or stale generated CSS.
- Broken dropdown, tooltip, or popover behavior usually means the Floating UI script is missing.
- Missing component types usually mean the Flowbite namespaces are not imported.
- Icon failures usually mean `Flowbite.Icons` or `Flowbite.Icons.Extended` is missing.
- Class conflicts usually mean the component’s merge helper or `Class` parameter was bypassed with manual string concatenation.

## Reference

- Component catalog and examples: [flowbite-blazor.llms.md (web version)](https://flowbite-blazor.org/llms-ctx.md)
- Official quickstart: https://flowbite-blazor.org/docs/getting-started/quickstart
