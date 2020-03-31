# CommandDotNet

## 3.5.1

Fix bug: when enabled w/ default configuration, Command Logger no longer outputs unless [cmdlog] is specified

## 3.5.0

### Feature

#### Command Logger

A middleware that will output...

* parse directive results
* optionally some system info: os version, .net version, machine name, ...
* optionally AppConfig
* can configure to include additional info with access to CommandContext

... and then run the command. Output can be forwarded to console or logs or ...

See [Command Logger help](command-logger.md) for more details

#### AppSettings.LongNameAlwaysDefaultsToSymbolName 

This setting will default to true in the next major version.

When setting `[Option(ShortName="a")]`, the LongName would no longer default to the parameter or property name and so you'd have to explicitely set the LongName if you wanted both. We want to change that behavior so that if you only want a short name, you'd need to `[Option(ShortName="a", LongName=null)]`.
AppSettings.LongNameAlwaysDefaultsToSymbolName allows us to introduce the behavior in a non-breaking manner until the next release.

[#183](https://github.com/bilal-fazlani/commanddotnet/issues/183)

## 3.4.0

### Feature

#### Enhanced Parse directive

The [parse directive](directives.md#parse) has been updated to show 

* argument values
* default values w/ sources 
* input values with sources, including response files if enabled.

Password values are replaced with ***** when the `Password` type is used.

### API

#### TokenCollection public ctor deprecated

Use Tokenizer.Tokenize extension method to generate tokens and TokenCollection.Transform to transform them

## 3.3.0

### Feature

#### Fix bug: support multiple default value config sources

There was a bug when using multiple config sources for default values, i.e. EnvVar and AppSettings.
The last one set would override previous ones. Resolved in this release.

### API

#### add ext method IArgument.IsObscured

Returns true if the type is `Password`

## 3.2.0

### API

#### Disable LogProvider by default

* LogProvider.IsDisabled is set to true in AppRunner static ctor.

#### [Guarantee Operand order in IArgumentModel](argument-models/#guaranteeing-the-order-of-operands)

The order of operands defined in IArgumentModel classes were never deterministic because .Net does not guarantee the order properties are reflected.

[CallerLineNumber] was used in the OperantAttribute ctor to ensure the order is always based on the order properties are defined in the class.

## 3.1.0

### Feature

#### Typo Suggestions enhancements

Support mis-ordered keywords: nameuser -> username

Trim results & improve accuracy

#### Support self-contained executable 

[AppInfo](https://github.com/bilal-fazlani/commanddotnet/blob/master/CommandDotNet/Builders/AppInfo.cs) interrogates the MainModule and EntryAssembly to determine if the app is run from `dotnet`, as a self-contained executable or a standard executable. This is used for help and to determine the correct app version.

[#197](https://github.com/bilal-fazlani/commanddotnet/issues/197), 
[#179](https://github.com/bilal-fazlani/commanddotnet/issues/179)

### API

#### AppInfo replaces VersionInfo

VersionInfo became a subset of features of AppInfo

#### IArgument.Default and ArgumentDefault

ArgumentDefault was introduced to track the source of a default value. We can determine if it came from source or an external source and the key used.
This is used in later versions for the Parse directive and CommandLogger feature.



## 3.0.2

### Feature

#### .UseDefaultMiddleware()

- added [Typo Suggestions](#typo-suggestions)

#### Typo Suggestions

[Typo Suggestions](typo-suggestions) middleware to suggest commands or options when a provided one wasn't found.

#### AppSettings.ExpandArgumentsInUsage

Expand arguments in the usage section so the names and order of all arguments are shown.

See [help docs](help.md#expandargumentsinusage) for more details.

[#186](https://github.com/bilal-fazlani/commanddotnet/issues/186), 

### API

#### Setting parent commands
Option & Operand & Command should now be created without a parent command. Parent will be assigned when added to a command.



## 3.0.1

### Feature

#### [%UsageAppName%](help.md#usageappname-tempate) 
To show usage examples in a command description, extended help or overridden usage section, use %UsageAppName%. This text will be replaced usage app name from one of the options above. See [the help docs](help.md#usageappname-tempate) for more.

### API

#### improve stack traces with AppOutputBase and PathMap project settings
```
<AppOutputBase>$(MSBuildProjectDirectory)\</AppOutputBase>
<PathMap>$(AppOutputBase)=CommandDotNet/</PathMap>
```