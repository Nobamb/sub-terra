# Changelog

## 1.0.0-mvp2

- Completed the MVP2 demo route through Surface Base, mine traversal, hazards, facilities, return, save, and progression.
- Added accessibility controls for reduced motion and safe-area aware menu layouts.
- Added repeatable Windows x64 Development, QA, and Release packaging gates.
- Reworked the start briefing (Prompt-B 120): terminal-signal entrance, low-intensity edge interference while open, 0.4s close, confirmed briefing text, and a pause gate that stops the mine reset timer and background input while it is open.
- Reworked the Clinic and Charger popups into compact cyan service panels with a heart + ECG (Clinic) or bolt + electric arc (Charger) entrance, a real HP/energy gauge showing the before/after values, and a short exit. Healing and charging still apply immediately; the animation is display-only and runs on unscaled time.
- Reworked the Outpost Core popup (Prompt-B 133) into a connected-facility list plus a live CCTV view rendered by one reusable preview camera. TV power-on / power-off animation (~1.3s in, ~0.26s out), fast eased camera moves with a CCTV-only afterimage, vertical scroll with keyboard selection follow, and live list updates while open. Facility connection, power, interaction range and save data are unchanged; selecting a facility only changes what the CCTV observes.

## Save compatibility

- Current save schema: v2.
- Game version is recorded in each save. Back up user saves before testing migrations or a new release build.
