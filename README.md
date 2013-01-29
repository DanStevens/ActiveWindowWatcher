ActiveWindowWatcher
=================== 

ActiveWindowWatcher class is a .NET component that provides access to currently
active window and triggers an event when it changes. This applies to all 
application running on the system, not just the host application. 

To use the ActiveWindowWatcher component, add the ActiveWindowWatcher.cs to your 
project. The component will be available in the Form Designer and can added to a 
form or other control. The Enabled property is set to false by default. Set this 
to true at design-time or run-time and the component will automatically respond 
to the appropriate Windows events and update the respective properties to 
correspond with the currently active window and will call the Changed event (if 
EnableRaisingEvents is true). 
