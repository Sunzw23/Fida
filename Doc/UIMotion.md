# UI motion

The Swiss palette and fixed grid remain the visual foundation. Animated edge
marks, a 320 ms page reveal, smooth button hover/press feedback, route reveals,
selected-skill bars, health interpolation and floating health changes provide
movement without additional assets or runtime dependencies. Result screens use
a brief geometric flourish. Existing card-action and number-flow effects remain.

Settings → Motion switches between Full and Reduced and persists in the existing
display settings file. Reduced mode skips page transitions, background movement,
route drawing, floating numbers and result flourishes; it also completes existing
card, number-flow and text-wipe animations immediately. Selection remains visible.

The new health animations update from the displayed health value so deferred
combat impacts retain their timing. No gameplay state or click bounds are changed.
Animations use elapsed time rather than frame counts. The page entrance detects
replacement of the first control, covering JSON menus, campaign screens and battles.

Validated with DesktopGL continuous-frame renders of the menu, settings and health
changes, regression tests, and a frame-rate/reduced-mode test. Android is compiled
but has not been checked on a physical device.
