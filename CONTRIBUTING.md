Get yourself a picture of the code conventions by just browsing the source code. Feel free to ask any question in [discord](https://discordapp.com/invite/tgAzGby).

## Code Conventions

1. Stick to the [Microsoft Code Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
2. Stick to the [Microsoft Secure Coding Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/secure-coding-guidelines)
3. Comment classes, methods and properties
4. Use HTML tags within the comments

```  
        /// <summary>
        /// Handles all default <see cref="Packet"/>s that are in the library.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be handled.</param>
        private void HandleDefaultPackets(Packet packet)
        {
```
5. Favor one-liners over multi-lines

```
        public virtual long RTT { get; protected set; } = 0;
        protected override void CloseHandler(CloseReason closeReason) => Close(closeReason, true);
```
6. An if/else with a one-line body has no brackets

```
        if (!packetPropertyCache.ContainsKey(type))
             packetPropertyCache[type] = PacketConverterHelper.GetTypeProperties(type);
```

7. Use regions for variables, properties, events and methods. Even add arbitrary regions to facilitate the  understandability.
8. Modifiers: private > protected > internal > public. Mark each class/property/method as close to the left as possible. Any internal logic shouldn't be visible to the public.

## PR-Requirements

1. SourceCode is buildable
2. Code Quality Factor equals the Master-Branch
3. Corresponds to one specific task/issue

## Don'ts

* Less is more. Large PR are likely to be rejected
* Reformat/refactor existing code; just for visuals
* PR without existing issue/task
* Addressing more than one issue/task
* Replacing or adding extrenal dependencies
