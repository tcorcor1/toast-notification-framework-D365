# Toast Notification Framework for Dynamics 365

- [Summary](#summary)
- [Demo](#demo)
- [Usage](#usage)

### Summary

My goal with this project was to create tool that non-developers could use to create, edit, dispatch and disable in-app/toast notifications in Dynamics 365.

For more information on the in-app notification feature please see the below links:

[MS Learn - Send in-app notifications](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/send-in-app-notifications)
[MS Learn - Notification (appnotification) table/entity reference](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/appnotification)

### Demo

<div>
  <img align="center" src="./docs/img/dataverse-analytics-147_013.gif" />
</div>

### Usage

# Enable the toast notification feature within model-driven apps

# Import solution

Managed and unmanaged solutions are included in ./solutions folder

# Provide security roles to users

Certain privileges are required in order to use in-app notifications in Dynamics. Those privileges are applied to roles included within the solutions folder so feel free to use those roles or merge with your own. The privileges for the Toast Notification Reader role are required to allow users to manage their in-app notification settings within a model-driven app. I recommend using Jonathan Daugaard's "Users, Team and Security Role Report" XrmToolBox plugin to bulk apply the reader/contributor roles as necessary.

- Toast Notification Administrator
- Toast Notification Contributor
- Toast Notification Reader
