# Changelog

## [1.5.3] - 06-04-24

- Fixed network desync in SyncPlayerList
- Increased sprite pixel units on UI atlas

## [1.5.2] - 04-18-24

- Changed protected void _DebugLog to void _AccessDebugLog in AccessEventBase
- Added optional color field to _Write method in DebugLog
- DebugLog prefab text changed to rich formatting by default
- Added BasicTest abstract class as standin for Func\<bool\>()

## [1.5.1] - 03-30-24

- Added virtual _OnInitHandlers method to EventBase that's called after handlers are initialized
- Fixed DebugState not adding context in all cases

## [1.5.0] - 03-28-24

- Fixed event handlers not always being called if same handler was re-entered
- Added eventDebuglog field to EventBase
- Added AccessEventBase class

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
