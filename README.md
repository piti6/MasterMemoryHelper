# MasterMemoryHelper
---
Generate MasterMemory required script or generate binary data from csv for one click.

Created for personal use.

Current version:
- MasterMemory -> https://github.com/Cysharp/MasterMemory/releases/tag/2.2.2
- MessagePack-CSharp -> https://github.com/neuecc/MessagePack-CSharp/releases/tag/v2.1.115

<br/><br/>

# QuickStart

---

<br/>

- ### Install

![image](https://user-images.githubusercontent.com/19143280/81118456-da95b500-8f63-11ea-8837-6cc3334f31cd.png)

Go to Window -> Package Manager on Unity then choose add package from git URL.

<br/>

- ### Create Configuration file

type https://github.com/piti6/MasterMemoryHelper.git and select add.

![image](https://user-images.githubusercontent.com/19143280/81118625-32ccb700-8f64-11ea-9622-abe325cf6839.png)

Create configuration asset for setup.

![image](https://user-images.githubusercontent.com/19143280/81119132-285eed00-8f65-11ea-8be8-30d56e4d037a.png)

All paths are relative path of Assets folder.
  - Script Input Path (Required)
    - Path for MasterMemory definition c# script.
  - Script Output Path (Required)
    - Path for Generated MasterMemory/MessagePack-CSharp Resolver/Formatter c# script.
  - Csv Input Path (Required)
    - Path for input csv data.
  - Binary Output Path (Required)
    - Path for binary data converted from csv. (used on runtime)
  - Namespace (Required)
    - Namespace for generated scripts. (both MasterMemory/MessagePack-CSharp namespace will affected)
  - Prefix Class Name (Optional)
    - Prefix class name for MasterMemory.

  - Database Builder Type (Required)
    - Type of custom DatabaseBuilder. if this is first time you imported package on project, you should generate DatabaseBuilder first.
    - After generate scripts by MasterMemoryHelper -> GenerateScripts, you should select type to use csv to binary converter.
  - Memory Database Type (Required)
    - Type of custom MemoryDatabase. if this is first time you imported package on project, you should generate MemoryDatabase first.
    - After generate scripts by MasterMemoryHelper -> GenerateScripts, you should select type to use csv to binary converter.

<br/>

- ### Usage

  - MasterMemoryHelper -> GenerateScripts
    - You should run this command when c# script definition file has changed.
  - MasterMemoryHelper -> GenerateBinaryFromCsv
    - You should run this command when csv data has changed.

<br/>

- ### Limitation

  - On script definition, Getter-Only property is only format that is currently supported.
(Cause currently I do not need to modify master data at all)
