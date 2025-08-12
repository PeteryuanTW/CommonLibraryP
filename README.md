### Environment
* Dotnet 8 Blazor Server-Side with Devexpress v25.1.x style
* SQL server 16+
### DB preparatory works
* Using MachineDB_Dev.sql or MapDB_Dev.sql to create necessary table schema.
* Check your SQL server can login with sql authentication.
### Code setting
appsettings.json
```
{
  "ConnectionStrings":
    {
      "connection string name": "connection string value" //Data Source=ip;Initial Catalog=db name;User ID=sa;Password=**********;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False
    },
  "othersetting"
  {
    ...
  },
  ...
}
```
# Machine package
### Use Case
> Simplifying the connection and communication to machine with common protocol.
Progeam.cs
```
builder.AddMachineService(); //if your connection string name is "DefaultConnection"
or
builder.AddMachineService(your connection string); //if your connection string name is not "DefaultConnection"
```
### Component setting
* Put following components in your page.
* Set machines and tags configuration in DB with following components.

```
<MachineSetting/>
<TagCategoriesSetting/>
```
### Verify setting
* Put following components in your page.
* Verify machine information is accessible with following component.
```
<MachineDashboard MachineId="@machineId"/>
```
### Verify service
* Inject MachineService to your own service (or component) which is already injected with singleton lifecycle in previous steps.
how to get tag value
```
Tag? tag = await MachineService.GetMachineTag(string machineName, string tagName);
Object? val = tag?.Value;//need to downcast by yourself
or
string s = tag?.ValueString();
```
how to set tag value
```
RequestResult res = await MachineService.SetMachineTagByString(string machineName, string tagName, string valString);
```
### Customize your machine status logic
status code equal oe less than 100 is preserved for system
dxButtonRenderStyle and Color only use for ui styling
status code and status name is not allow to be duplicate
```
CommonEnumHelper.AddCustomStatus(status code, status name, dxButtonRenderStyle, Color);
```

write your own machine class
inherit ModbusTCPMachine and overide function UpdateStatus
define connection type code 0 and 1 is preserved for system
connection type code is not allow to be duplicate
```
MachineTypeEnumHelper.AddCustomConnection<YourMachineClass>(your connection type code);
```
# Map package
### Use Case
> Visualize machine in your image as a dashboard

### Component setting
* Put following components in your page
* Set map configuration and map components in DB with following components

```
<MapSetting/>
<MapEditor MapConfigId="@mapId/>
```

### Verify setting
* Put following components in your page
* Verify mpa information
```
<MapDashboard MapConfigParam="@MapConfigParam"/>
```
