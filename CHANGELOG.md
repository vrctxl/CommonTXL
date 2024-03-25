# Changelog

## [1.4.0] - 03-24-24

- Added virtual _PreInit method to EventBase that's called before initializing handlers
- Added virtual _PostInit method to EventBase that's called on next frame after _Init
- Added virtual _OnRegister and _OnUnregister internal callbacks to EventBase
- Added virtual _PreInit method to ControlBase that's called before initializing controls
- Added virtual _PostInit method to ControlBase that's called on next frame after _Init
- Added _SetButton variant to ControlBase that takes color index
- Added purple as default color to ControlBase
- Changed DebugState to extend EventBase
- Added _SetContext method to DebugState to add context after registering normal event handler
- Added _ContainsPlayer and _ContainsAnyPlayerInWorld methods to AccessControlUserSource abstract class
- Added "loop 1" icon to UI atlas
- Added MenuUtil class for editor support

## [1.3.0] - 02-11-24

- CAUTION: Changed AccessControl to manual network sync
- Added allowFirstJoin and restrictFirstJoinIfOwnerPresent options to AccessControl
- Added TextMeshProGUI support to Button handlers in ControlBase
- Included Udon Tools

## [1.2.0] - 12-29-23

- BREAKING: Removed _RegisterAccessHandler method from AccessControl
- BREAKING: Removed RESULT_ALLOW, RESULT_DENY, RESULT_PASS constants from AccessControl
- Added _AddAccessHandler method to AccessControl
- Added accessHandlers field to AccessControl
- Added AccessControlHandler base class for all access handler implementations to extend from
- Added default GraphAPI child game object to AccessControl prefab 

## [1.1.0] - 11-25-23

- Added _Unregister support to EventBase to detach event handlers
- Fixed event handlers to allow multiple registrations by the same component
- Event calls can now be handled recursively
- EventBase will reject registering or unregistering handlers if already in an event call
- Fixed AccessControl _Validate method to also refresh its whitelist status

## [1.0.3] - 09-22-23

- Fix crash in EventBase if events are fired before any handlers are registered
- Removed depdency on UdonSharp package

## [1.0.2] - 08-31-23

- Add Runtime/Scripts/AccessControlGraphAPI to expose a usable Access Control API to Udon Graph

## [1.0.1] - 08-06-23

- Add _AddLocalPlayer and _RemoveLocalPlayer methods to ZoneMembership

## [1.0.0] - 07-29-23

- First post-VPM release
