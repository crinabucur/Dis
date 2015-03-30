      *** IMPORTANT NOTE: 
	  Changes in Web.config and in the configuration file:
      Since version 1.9.2 the section name in Web.config has changed from name="fileUpload" to name="backload" (see below). 
      The root element must also be changed in your config file from <fileUpload> to <backload>
      The ConfigurationSection class has changed to <section type="Backload.Configuration.BackloadSection ..." />
	  Backload has implemented a fallback routine for the old schema, but it is best practice to update your config files.
	  See examples on GitHub.
	  
	  Release notes 1.9.3.1:
	  Bug fixes:
	  - Issue #28 resolved, where an exception occured in MVC5 RC with an "Attempt to access security transparent method" violation.
	  - Issue #30 resolved, where an exception occured with an authenticated user and forms authentication without role provider.
	  - Issue with Entity Framwork Migrations resolved which prevented EF Migrations to run properly.
	  - Stackoverflow #18976744: System Web Optimization reference set back to 1.0.0 and above which is the default in MVC4.
