This project is driven from [RTSCamera](https://github.com/densylkin/RTSCamera),you can also find it at asset store:[RTS camera](https://assetstore.unity.com/packages/tools/camera/rts-camera-43321).
## Summary
It's a good camera control package for RTS style game with clean code.But the author have not update it for a long time.So there are some aspects need update.
- 1,Parameter autoHeight never used,and height's initial is minHeight.
- 2,EditorGUILayout.ObjectField is obsolete.
- 3,Follow target works weird, target may not in the camera's view.
- 4,Physics.Raycast(ray, out hit, groundMask.value) not works as author's initial intention with current new unity version.
- 5,Use parameter limitX and Limit Y to restrict map, author treats the map as square.

## Changes
### v1.0 2023/01/05
- 1,Rename ``autoHeight`` with ``initWithOriginHeight``。Default camera's init view is what you see in your editor view.If initWithOriginHeight is set false, the camera will move to minHeight's associate pos.
- 2,Rectify EditorGUILayout.ObjectFiled
- 3,Follow target now works better
- 4,Modify Physics.Raycast
- 5,Use limitXMin、limitXMax、limitYMin、limitYMax to restrict map area
