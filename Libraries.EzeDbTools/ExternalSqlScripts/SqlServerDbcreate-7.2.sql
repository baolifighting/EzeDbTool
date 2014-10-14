

CREATE TABLE alarms (
  alarmId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  [description] nvarchar(128) default NULL,
  PRIMARY KEY  (alarmId)
);

CREATE TABLE roles (
  name nvarchar(50) default NULL,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  protected tinyint NOT NULL default 0,
  hideRole tinyint default 0,
  visibilityflags bigint NOT NULL default 7,
  PRIMARY KEY  ([guid]),
  CONSTRAINT guid UNIQUE ([guid]),
  CONSTRAINT role_name_unique UNIQUE (name)
);

CREATE TABLE users (
  userid bigint default NULL,
  userName nvarchar(36) NOT NULL,
  passwd nvarchar(50) NOT NULL default '',
  firstName nvarchar(50) NOT NULL default '',
  lastname nvarchar(50) NOT NULL default '',
  deleted tinyint NOT NULL default 0,
  [timeout] bigint NOT NULL default 0,
  expires tinyint NOT NULL default 0,
  [expiredate] datetime2 NOT NULL default '0001-01-01 00:00:00',
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  passwdChanged datetime2 NOT NULL default '0001-01-01 00:00:00',
  passwordviolations tinyint NOT NULL default 0,
  lastsignindate datetime2 NOT NULL default '0001-01-01 00:00:00',
  roleguid varbinary(16) default NULL,
  [sid] nvarchar(64) default NULL,
  PRIMARY KEY  ([guid]),
  CONSTRAINT user_name_unique UNIQUE(userName),
  CONSTRAINT FK1_users_roles FOREIGN KEY (roleguid) REFERENCES roles ([guid])
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'users_userid_dummy_idx')
   DROP INDEX users.users_userid_dummy_idx
;
CREATE INDEX users_userid_dummy_idx ON users(userid)

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_users_roles')
   DROP INDEX users.FK1_users_roles
;
CREATE INDEX FK1_users_roles ON users(roleguid);

CREATE TABLE appliances (
  applianceid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) NOT NULL,
  hostaddress nvarchar(255) default NULL,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  deleted tinyint NOT NULL default 0,
  unavailable tinyint NOT NULL default 0,
  ishost tinyint NOT NULL default 0,
  machineType bigint NOT NULL default 1,
  license nvarchar(255) default NULL,
  [status] bigint NOT NULL default 0,
  requestedStatus bigint NOT NULL default 0,
  installdate datetime2 NOT NULL default '0001-01-01 00:00:00',
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  updated datetime2 NOT NULL default '0001-01-01 00:00:00',
  videostart datetime2 NOT NULL default '0001-01-01 00:00:00',
  syncdate datetime2 NOT NULL default '0001-01-01 00:00:00',
  timezonekey nvarchar(128) default '',
  softwareversion nvarchar(16) default NULL,
  hardwareversion nvarchar(16) default NULL,
  partnumber nvarchar(32) default NULL,
  serialnumber nvarchar(32) default NULL,
  enterpriseInfoPushed tinyint default 0,
  enterprisedbyuserguid varbinary(16) default NULL,
  PRIMARY KEY  (applianceid),
  CONSTRAINT alliance_name_unique UNIQUE(name),
  CONSTRAINT guids UNIQUE([guid]),
  CONSTRAINT FK1_appliances_users FOREIGN KEY (enterprisedbyuserguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'appliances_hostaddress')
   DROP INDEX appliances.appliances_hostaddress
;
CREATE INDEX appliances_hostaddress
	 ON appliances(hostaddress)
;
IF EXISTS (SELECT name FROM sysindexes WHERE name = 'appliances_ishost')
   DROP INDEX appliances.appliances_ishost
;
CREATE INDEX appliances_ishost ON appliances(ishost)
;
IF EXISTS (SELECT name FROM sysindexes WHERE name = 'appliances_serialnumber')
   DROP INDEX appliances.appliances_serialnumber
;
CREATE INDEX appliances_serialnumber ON appliances(serialnumber)
;
IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_appliances_users')
   DROP INDEX appliances.FK1_appliances_users
;
CREATE INDEX FK1_appliances_users ON appliances(enterprisedbyuserguid)
;

CREATE TABLE alarmdevices (
  alarmdeviceid bigint NOT NULL IDENTITY(1,1),
  applianceId bigint NOT NULL default 0,
  [type] int NOT NULL,
  name nvarchar(50) default NULL,
  [UID] nvarchar(25) default NULL,
  available tinyint NOT NULL default 1,
  PRIMARY KEY  (alarmdeviceid),
  CONSTRAINT FK1_AlarmDevices_Appliances FOREIGN KEY (applianceId) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_AlarmDevices_Appliances')
   DROP INDEX alarmdevices.FK1_AlarmDevices_Appliances
;
CREATE INDEX FK1_AlarmDevices_Appliances ON alarmdevices(applianceId)
;

CREATE TABLE alarmdeviceports (
  alarmdeviceportid bigint NOT NULL IDENTITY(1,1),
  [UID] bigint NOT NULL default 0,
  alarmdeviceid bigint default NULL,
  [type] bigint default NULL,
  PRIMARY KEY  (alarmdeviceportid),
  CONSTRAINT FK1_AlarmDevicePorts_AlarmDevices FOREIGN KEY (alarmdeviceid) REFERENCES alarmdevices (alarmdeviceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_AlarmDevicePorts_AlarmDevices')
   DROP INDEX alarmdeviceports.FK1_AlarmDevicePorts_AlarmDevices
;
CREATE INDEX FK1_AlarmDevicePorts_AlarmDevices ON alarmdeviceports(alarmdeviceid)
;

CREATE TABLE channeldrivers (
  channelDriverId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  channelType int NOT NULL default 0,
  resourceUnits float NOT NULL default 0,
  licenseUnits float NOT NULL default 0,
  configurationXml nvarchar(max),
  channelinputdriverguid varbinary(16) default NULL,
  videodriverguid varbinary(16) default NULL,
  ptzdriverguid varbinary(16) default NULL,
  streamedvideodriverguid varbinary(16) default NULL,
  channelsettingsdriverguid varbinary(16) default NULL,
  PRIMARY KEY  (channelDriverId)
);

CREATE TABLE channels (
  channelid bigint NOT NULL IDENTITY(1,1),
  edgenodeid bigint default NULL,
  name nvarchar(255) default NULL,
  number bigint default NULL,
  channelType int NOT NULL default 1,
  channelDriverId bigint default NULL,
  identifier nvarchar(255) default NULL,
  isActive tinyint NOT NULL default 1,
  isHidden tinyint NOT NULL default 0,
  defaultControlPatternId bigint default NULL,
  applianceid bigint default NULL,
  isoverlay tinyint NOT NULL default 0,
  ptzstate tinyint NOT NULL default 0,
  localid bigint NOT NULL default 0,
  visibility bigint NOT NULL default 1,
  PRIMARY KEY  (channelid),
  CONSTRAINT FK1_Channels_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT FK1_Channels_ChannelDrivers FOREIGN KEY (channelDriverId) REFERENCES channeldrivers (channelDriverId)
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_Channels_Appliances')
   DROP INDEX channels.FK1_Channels_Appliances
;
CREATE INDEX FK1_Channels_Appliances ON channels(applianceid)
;
IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_Channels_ChannelsControlPatterns')
   DROP INDEX channels.FK1_Channels_ChannelsControlPatterns
;
CREATE INDEX FK1_Channels_ChannelsControlPatterns ON channels(defaultControlPatternId)
;
IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_Channels_ChannelDrivers')
   DROP INDEX channels.FK1_Channels_ChannelDrivers
;
CREATE INDEX FK1_Channels_ChannelDrivers ON channels(channelDriverId)
;

CREATE TABLE channelcontrolpatterns (
  channelControlPatternId bigint NOT NULL IDENTITY(1,1),
  channelid bigint NOT NULL default 0,
  name nvarchar(100) default NULL,
  controlPatternXml nvarchar(max),
  PRIMARY KEY  (channelControlPatternId),
  CONSTRAINT FK1_ChannelControlPatterns_Channel FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_ChannelControlPatterns_Channel')
   DROP INDEX channelcontrolpatterns.FK1_ChannelControlPatterns_Channel
;
CREATE INDEX FK1_ChannelControlPatterns_Channel ON channelcontrolpatterns(channelid)
;
--change into trigger
ALTER TABLE channels  ADD CONSTRAINT FK1_Channels_ChannelsControlPatterns FOREIGN KEY (defaultControlPatternId) REFERENCES channelcontrolpatterns(channelControlPatternId)
;
CREATE TABLE alarmportchannels (
  alarmportchannelid bigint NOT NULL IDENTITY(1,1),
  [UID] bigint NOT NULL default 0,
  alarmdeviceportid bigint default NULL,
  channelid bigint default NULL,
  name nvarchar(50) default NULL,
  normallyOpen tinyint default NULL,
  [status] tinyint default NULL,
  PRIMARY KEY  (alarmportchannelid),
-- change to trigger
  CONSTRAINT FK1_AlarmPortChannels_AlarmDevicePorts FOREIGN KEY (alarmdeviceportid) REFERENCES alarmdeviceports (alarmdeviceportid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_AlarmPortChannels_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) 
);

IF EXISTS (SELECT name FROM sysindexes WHERE name = 'FK1_AlarmPortChannels_AlarmDevicePorts')
	DROP INDEX alarmportchannels.FK1_AlarmPortChannels_AlarmDevicePorts
;
CREATE INDEX FK1_AlarmPortChannels_AlarmDevicePorts
	 ON alarmportchannels(alarmdeviceportid)
;
IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlarmPortChannels_Channels')
   DROP INDEX alarmportchannels.FK1_AlarmPortChannels_Channels
;
CREATE INDEX FK1_AlarmPortChannels_Channels
	 ON alarmportchannels(channelid);


CREATE TABLE alarmtriggers (
  alarmTriggerId bigint NOT NULL IDENTITY(1,1),
  alarmportchannelid bigint default NULL,
  duration bigint default NULL,
  PRIMARY KEY  (alarmTriggerId),
  CONSTRAINT FK1_AlarmTriggers_AlarmPortChannels FOREIGN KEY (alarmportchannelid) REFERENCES alarmportchannels (alarmportchannelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlarmTriggers_AlarmPortChannels')
   DROP INDEX alarmtriggers.FK1_AlarmTriggers_AlarmPortChannels
;
CREATE INDEX FK1_AlarmTriggers_AlarmPortChannels
	 ON alarmtriggers(alarmportchannelid);

CREATE TABLE alarmactions (
  alarmActionId bigint NOT NULL IDENTITY(1,1),
  alarmId bigint NOT NULL default 0,
  alarmTriggerId bigint NOT NULL default 0,
  PRIMARY KEY  (alarmActionId),
  CONSTRAINT FK1_AlarmActions_Alarms FOREIGN KEY (alarmId) REFERENCES alarms (alarmId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_AlarmActions_AlarmTriggers FOREIGN KEY (alarmTriggerId) REFERENCES alarmtriggers (alarmTriggerId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlarmActions_Alarms')
   DROP INDEX alarmactions.FK1_AlarmActions_Alarms
;
CREATE INDEX FK1_AlarmActions_Alarms
	 ON alarmactions(alarmId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlarmActions_AlarmTriggers')
   DROP INDEX alarmactions.FK1_AlarmActions_AlarmTriggers
;
CREATE INDEX FK1_AlarmActions_AlarmTriggers
	 ON alarmactions(alarmTriggerId);

CREATE TABLE savedsearches (
  savedSearchId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  [description] nvarchar(128) default NULL,
  searchType bigint NOT NULL default 0,
  predicateXml nvarchar(max),
  servers nvarchar(max),
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (savedSearchId),
  CONSTRAINT FK1_savedsearches_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_savedsearches_users')
   DROP INDEX savedsearches.FK1_savedsearches_users
;
CREATE INDEX FK1_savedsearches_users
	 ON savedsearches(userguid);

CREATE TABLE alerts (
  alertId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  [description] nvarchar(128) default NULL,
  note nvarchar(max),
  noteTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  color bigint NOT NULL default 0,
  isEnabled tinyint NOT NULL default 0,
  savedSearchId bigint NOT NULL default 0,
  hideAlert tinyint default 0,
  isShared tinyint NOT NULL default 0,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  applianceid bigint default NULL,
  PRIMARY KEY  (alertId),
  CONSTRAINT FK1_Alerts_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON UPDATE CASCADE,
  CONSTRAINT FK1_Alerts_SavedSearches FOREIGN KEY (savedSearchId) REFERENCES savedsearches (savedSearchId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Alerts_SavedSearches')
   DROP INDEX alerts.FK1_Alerts_SavedSearches
;
CREATE INDEX FK1_Alerts_SavedSearches
	 ON alerts(savedSearchId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Alerts_Appliances')
   DROP INDEX alerts.FK1_Alerts_Appliances
;
CREATE INDEX FK1_Alerts_Appliances
	 ON alerts(applianceid);

CREATE TABLE alertalarms (
  alertAlarmId bigint NOT NULL IDENTITY(1,1),
  alertId bigint NOT NULL default 0,
  alarmId bigint default NULL,
  PRIMARY KEY  (alertAlarmId),
  CONSTRAINT FK1_AlertAlarms_Alarms FOREIGN KEY (alarmId) REFERENCES alarms (alarmId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_AlertAlarms_Alerts FOREIGN KEY (alertId) REFERENCES alerts (alertId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertAlarms_Alerts')
   DROP INDEX alertalarms.FK1_AlertAlarms_Alerts
;
CREATE INDEX FK1_AlertAlarms_Alerts
	 ON alertalarms(alertId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertAlarms_Alarms')
   DROP INDEX alertalarms.FK1_AlertAlarms_Alarms
;
CREATE INDEX FK1_AlertAlarms_Alarms
	 ON alertalarms(alarmId);

CREATE TABLE alertchannels (
  alertchannelid bigint NOT NULL IDENTITY(1,1),
  alertId bigint NOT NULL default 0,
  channelid bigint default NULL,
  PRIMARY KEY  (alertchannelid),
  CONSTRAINT FK1_AlertChannels_Alerts FOREIGN KEY (alertId) REFERENCES alerts (alertId),
  CONSTRAINT FK1_AlertChannels_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertChannels_Channels')
   DROP INDEX alertchannels.FK1_AlertChannels_Channels
;
CREATE INDEX FK1_AlertChannels_Channels
	 ON alertchannels(channelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertChannels_Alerts')
   DROP INDEX alertchannels.FK1_AlertChannels_Alerts
;
CREATE INDEX FK1_AlertChannels_Alerts
	 ON alertchannels(alertId);

CREATE TABLE emails (
  emailId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  [subject] nvarchar(128) default NULL,
  body nvarchar(max),
  alertName tinyint default NULL,
  alertDetails tinyint default NULL,
  oneFrame tinyint default NULL,
  allFrames tinyint default NULL,
  video tinyint default NULL,
  sendearlyemail tinyint default NULL,
  annotateImages tinyint NOT NULL default 0,
  sendverificationimage tinyint default 0,
  audio tinyint default 0,
  PRIMARY KEY  (emailId)
);

CREATE TABLE alertemails (
  alertEmailId bigint NOT NULL IDENTITY(1,1),
  alertId bigint NOT NULL default 0,
  emailId bigint default NULL,
  PRIMARY KEY  (alertEmailId),
  CONSTRAINT FK1_AlertEmails_Alerts FOREIGN KEY (alertId) REFERENCES alerts (alertId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_AlertEmails_Emails FOREIGN KEY (emailId) REFERENCES emails (emailId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertEmails_Emails')
   DROP INDEX alertemails.FK1_AlertEmails_Emails
;
CREATE INDEX FK1_AlertEmails_Emails
	 ON alertemails(emailId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertEmails_Alerts')
   DROP INDEX alertemails.FK1_AlertEmails_Alerts
;
CREATE INDEX FK1_AlertEmails_Alerts
	 ON alertemails(alertId);

CREATE TABLE alertmotionregions (
  alertMotionRegionId bigint NOT NULL IDENTITY(1,1),
  alertId bigint NOT NULL default 0,
  uLX bigint NOT NULL default 0,
  uLY bigint NOT NULL default 0,
  lRX bigint NOT NULL default 0,
  lRY bigint NOT NULL default 0,
  directions bigint NOT NULL default 0,
  PRIMARY KEY  (alertMotionRegionId),
  CONSTRAINT FK1_AlertMotionRegions_Alerts FOREIGN KEY (alertId) REFERENCES alerts (alertId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_AlertMotionRegions_Alerts')
		  DROP INDEX alertmotionregions.FK1_AlertMotionRegions_Alerts
;
CREATE INDEX FK1_AlertMotionRegions_Alerts
		  ON alertmotionregions(alertId);

CREATE TABLE valuesets (
  valuesetid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  PRIMARY KEY  (valuesetid)
);

CREATE TABLE parameterchanges (
  parameterchangeid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  prealertrecordingperiod bigint default 0,
  postalertrecordingperiod bigint default 0,
  valuesetid bigint default NULL,
  PRIMARY KEY  (parameterchangeid),
  CONSTRAINT fk1_parameterchange_valuesets FOREIGN KEY (valuesetid) REFERENCES valuesets (valuesetid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk1_parameterchange_valuesets')
   DROP INDEX parameterchanges.fk1_parameterchange_valuesets
;
CREATE INDEX fk1_parameterchange_valuesets
	 ON parameterchanges(valuesetid);

CREATE TABLE alertparameterchanges (
  alertparameterchangeid bigint NOT NULL IDENTITY(1,1),
  alertid bigint default NULL,
  parameterchangeid bigint default NULL,
  PRIMARY KEY  (alertparameterchangeid),
  CONSTRAINT fk1_alertparameterchange_alerts FOREIGN KEY (alertid) REFERENCES alerts (alertId) ON DELETE CASCADE,
  CONSTRAINT fk1_alertparameterchange_parameterchanges FOREIGN KEY (parameterchangeid) REFERENCES parameterchanges (parameterchangeid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk1_alertparameterchange_alerts')
   DROP INDEX alertparameterchanges.fk1_alertparameterchange_alerts
;
CREATE INDEX fk1_alertparameterchange_alerts
	 ON alertparameterchanges(alertid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk1_alertparameterchange_parameterchanges')
   DROP INDEX alertparameterchanges.fk1_alertparameterchange_parameterchanges
;
CREATE INDEX fk1_alertparameterchange_parameterchanges
	 ON alertparameterchanges(parameterchangeid);

CREATE TABLE components (
  componentId bigint NOT NULL IDENTITY(1,1),
  scopeId bigint default NULL,
  componentNumber bigint default NULL,
  PRIMARY KEY  (componentId)
);

CREATE TABLE scopes (
  scopeId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  [type] tinyint NOT NULL default 0,
  PRIMARY KEY  (scopeId)
);

CREATE TABLE settingcategories (
  settingCategoryId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  screenOrder int NOT NULL default 0,
  PRIMARY KEY  (settingCategoryId)
);

CREATE TABLE settings (
  settingId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  settingCategoryId bigint default NULL,
  scopeId bigint default NULL,
  screenOrder int NOT NULL default 0,
  isTemplatable tinyint NOT NULL default 1,
  PRIMARY KEY  (settingId),
  CONSTRAINT FK1_Settings_Scopes FOREIGN KEY (scopeId) REFERENCES scopes (scopeId),
  CONSTRAINT FK1_Settings_SettingCategories FOREIGN KEY (settingCategoryId) REFERENCES settingcategories (settingCategoryId)
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Settings_SettingCategories')
   DROP INDEX settings.FK1_Settings_SettingCategories
;
CREATE INDEX FK1_Settings_SettingCategories
	 ON settings(settingCategoryId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Settings_Scopes')
   DROP INDEX settings.FK1_Settings_Scopes
;
CREATE INDEX FK1_Settings_Scopes
	 ON settings(scopeId);

CREATE TABLE settingdefinitions (
  settingDefinitionId bigint NOT NULL IDENTITY(1,1),
  settingId bigint default NULL,
  channelDriverId bigint default NULL,
  factoryDefaultValue nvarchar(max),
  templateDefaultValue nvarchar(max),
  restoreDefault tinyint NOT NULL default 1,
  minval nvarchar(16) default NULL,
  maxval nvarchar(16) default NULL,
  dataType tinyint NOT NULL default 0,
  accessLevel tinyint NOT NULL default 2,
  [activation] tinyint NOT NULL default 1,
  internalIdentifier nvarchar(50) default NULL,
  licenseVal nvarchar(max) NOT NULL,
  isReadOnly tinyint default 0,
  isVisible tinyint default 1,
  PRIMARY KEY  (settingDefinitionId),
  CONSTRAINT FK1_SettingDefinitions_ChannelDrivers FOREIGN KEY (channelDriverId) REFERENCES channeldrivers (channelDriverId) ON DELETE CASCADE,
  CONSTRAINT FK1_SettingDefinitions_Settings FOREIGN KEY (settingId) REFERENCES settings (settingId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'IDX_Settings_name')
   DROP INDEX settings.IDX_Settings_name
;
CREATE INDEX IDX_Settings_name
	 ON settings(name);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'IDX_channeldrivers_name')
   DROP INDEX channeldrivers.IDX_channeldrivers_name
;
CREATE INDEX IDX_channeldrivers_name
	 ON channeldrivers(name);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_SettingDefinitions_Settings')
   DROP INDEX settingdefinitions.FK1_SettingDefinitions_Settings
;
CREATE INDEX FK1_SettingDefinitions_Settings
	 ON settingdefinitions(settingId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_SettingDefinitions_ChannelDrivers')
   DROP INDEX settingdefinitions.FK1_SettingDefinitions_ChannelDrivers
;
CREATE INDEX FK1_SettingDefinitions_ChannelDrivers
	 ON settingdefinitions(channelDriverId);

CREATE TABLE settingvalues (
  settingValueId bigint NOT NULL IDENTITY(1,1),
  value nvarchar(MAX),
  shouldIgnore tinyint NOT NULL default 0,
  completionStatus tinyint NOT NULL default 0,
  componentId bigint default NULL,
  settingDefinitionId bigint default NULL,
  PRIMARY KEY  (settingValueId),
  CONSTRAINT FK1_SettingValues_Components FOREIGN KEY (componentId) REFERENCES components (componentId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_SettingValues_SettingDefinitions FOREIGN KEY (settingDefinitionId) REFERENCES settingdefinitions (settingDefinitionId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_SettingValues_Components')
   DROP INDEX settingvalues.FK1_SettingValues_Components
;
CREATE INDEX FK1_SettingValues_Components
	 ON settingvalues(componentId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_SettingValues_SettingDefinitions')
   DROP INDEX settingvalues.FK1_SettingValues_SettingDefinitions
;
CREATE INDEX FK1_SettingValues_SettingDefinitions
	 ON settingvalues(settingDefinitionId);

CREATE TABLE alternatesettingvalues (
  valuesetid bigint NOT NULL,
  alternatesettingvalueid bigint NOT NULL IDENTITY(1,1),
  settingvalueid bigint NOT NULL,
  setting nvarchar(max),
  PRIMARY KEY  (alternatesettingvalueid),
  CONSTRAINT fk1_alternatesettingvalue_settingvalues FOREIGN KEY (settingvalueid) REFERENCES settingvalues (settingValueId) ON DELETE CASCADE,
  CONSTRAINT fk1_alternatesettingvalue_valuesets FOREIGN KEY (valuesetid) REFERENCES valuesets (valuesetid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk1_alternatesettingvalue_valuesets')
   DROP INDEX alternatesettingvalues.fk1_alternatesettingvalue_valuesets
;
CREATE INDEX fk1_alternatesettingvalue_valuesets
	 ON alternatesettingvalues(valuesetid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk1_alternatesettingvalue_settingvalues')
   DROP INDEX alternatesettingvalues.fk1_alternatesettingvalue_settingvalues
;
CREATE INDEX fk1_alternatesettingvalue_settingvalues
	 ON alternatesettingvalues(settingvalueid);

CREATE TABLE metadatakeys (
  metadataKeyId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  [description] nvarchar(120) default NULL,
  [type] bigint default NULL,
  allowPersonJoin tinyint NOT NULL default 0,
  allowuseredit tinyint NOT NULL default 0,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  isSystemKey tinyint NOT NULL default 0,
  displayPosition smallint NOT NULL default 0,
  [format] nvarchar(255) NOT NULL default '',
  allowApplianceJoin tinyint NOT NULL default 0,
  allowChannelJoin tinyint NOT NULL default 0,
  allowmotionevents tinyint NOT NULL default 0,
  allowfaceevents tinyint NOT NULL default 0,
  allowimportedimageevents tinyint NOT NULL default 0,
  allowgenericevents tinyint NOT NULL default 0,
  isOverlay tinyint NOT NULL default 0,
  streamtimeusage tinyint NOT NULL default 0,
  isMultiple tinyint NOT NULL default 0,
  PRIMARY KEY  (metadataKeyId),
   CONSTRAINT metadatakeys_guid UNIQUE ([guid])
);

CREATE TABLE metadataelements (
  metadataElementId bigint NOT NULL IDENTITY(1,1),
  metadataKeyId bigint NOT NULL default 0,
  begintime datetime2 NOT NULL default '0001-01-01 00:00:00',
  beginMilliseconds bigint NOT NULL default 0,
  endtime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endMilliseconds bigint NOT NULL default 0,
  PRIMARY KEY  (metadataElementId),
  CONSTRAINT FK1_MetadataElements_MetadataKeys FOREIGN KEY (metadataKeyId) REFERENCES metadatakeys (metadataKeyId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataElements_MetadataKeys')
   DROP INDEX metadataelements.FK1_MetadataElements_MetadataKeys
;
CREATE INDEX FK1_MetadataElements_MetadataKeys
	 ON metadataelements(metadataKeyId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'begintime')
   DROP INDEX metadataelements.begintime
;
CREATE INDEX begintime
	 ON metadataelements(begintime);

CREATE TABLE appliancemetadataelements (
  applianceMetadataElementId bigint NOT NULL IDENTITY(1,1),
  applianceId bigint default NULL,
  metadataElementId bigint default NULL,
  PRIMARY KEY  (applianceMetadataElementId),
  CONSTRAINT FK1_ApplianceMetadataElements_Appliances FOREIGN KEY (applianceId) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_ApplianceMetadataElements_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ApplianceMetadataElements_Appliances')
   DROP INDEX appliancemetadataelements.FK1_ApplianceMetadataElements_Appliances
;
CREATE INDEX FK1_ApplianceMetadataElements_Appliances
	 ON appliancemetadataelements(applianceId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ApplianceMetadataElements_MetadataElements')
   DROP INDEX appliancemetadataelements.FK1_ApplianceMetadataElements_MetadataElements
;
CREATE INDEX FK1_ApplianceMetadataElements_MetadataElements
	 ON appliancemetadataelements(metadataElementId);

CREATE TABLE appliancetemplatesxml (
  appliancetemplatexmlid bigint NOT NULL IDENTITY(1,1),
  [xml] nvarchar(max) NOT NULL,
  PRIMARY KEY  (appliancetemplatexmlid)
);

CREATE TABLE appliancetemplates (
  appliancetemplateid bigint NOT NULL IDENTITY(1,1),
  appliancetemplatexmlid bigint default NULL,
  name nvarchar(50) NOT NULL default '',
  [description] nvarchar(max),
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  hardware nvarchar(7) default NULL,
  isbackup tinyint default 0,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (appliancetemplateid),
  CONSTRAINT FK1_appliancetemplates_users FOREIGN KEY (userguid) REFERENCES users ([guid]),
  CONSTRAINT FK_appliancetemplates_appliancetemplatexml FOREIGN KEY (appliancetemplatexmlid) REFERENCES appliancetemplatesxml (appliancetemplatexmlid) ON DELETE CASCADE ON UPDATE NO ACTION
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK_appliancetemplates_appliancetemplatexml')
   DROP INDEX appliancetemplates.FK_appliancetemplates_appliancetemplatexml
;
CREATE INDEX FK_appliancetemplates_appliancetemplatexml
	 ON appliancetemplates(appliancetemplatexmlid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_appliancetemplates_users')
   DROP INDEX appliancetemplates.FK1_appliancetemplates_users
;
CREATE INDEX FK1_appliancetemplates_users
	 ON appliancetemplates(userguid);

CREATE TABLE devices (
  deviceId bigint NOT NULL IDENTITY(1,1),
  uri nvarchar(max),
  [status] nvarchar(50) default NULL,
  PRIMARY KEY  (deviceId)
);

CREATE TABLE audiorecording (
  audiorecordingid bigint NOT NULL IDENTITY(1,1),
  channelid bigint NOT NULL,
  audiochannelid bigint NOT NULL,
  deviceid bigint NOT NULL,
  starttime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endtime datetime2 NOT NULL default '9999-12-31 23:59:59',
  PRIMARY KEY  (audiorecordingid),
  CONSTRAINT FK1_audiorecording_AudioChannel FOREIGN KEY (audiochannelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE,
-- change to trigger
 CONSTRAINT FK1_audiorecording_Channel FOREIGN KEY (channelid) REFERENCES channels (channelid) ,
  CONSTRAINT FK1_audiorecording_Device FOREIGN KEY (deviceid) REFERENCES devices (deviceId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_audiorecording_Channel')
   DROP INDEX audiorecording.FK1_audiorecording_Channel
;
CREATE INDEX FK1_audiorecording_Channel
	 ON audiorecording(channelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_audiorecording_AudioChannel')
   DROP INDEX audiorecording.FK1_audiorecording_AudioChannel
;
CREATE INDEX FK1_audiorecording_AudioChannel
	 ON audiorecording(audiochannelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_audiorecording_Device')
   DROP INDEX audiorecording.FK1_audiorecording_Device
;
CREATE INDEX FK1_audiorecording_Device
	 ON audiorecording(deviceid);

CREATE TABLE casestatuses (
  caseStatusId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  PRIMARY KEY  (caseStatusId)
);

CREATE TABLE cases (
  caseId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  number nvarchar(50) default NULL,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  creationdate datetime2 NOT NULL default '0001-01-01 00:00:00',
  lastmodifieddate datetime2 NOT NULL default '0001-01-01 00:00:00',
  caseStatusId bigint NOT NULL default 0,
  isprivate tinyint default 0,
  [description] nvarchar(max),
  userguid varbinary(16) default NULL,
  assigneduserguid varbinary(16) default NULL,
  PRIMARY KEY  (caseId),
  CONSTRAINT FK1_Cases_CaseStatuses FOREIGN KEY (caseStatusId) REFERENCES casestatuses (caseStatusId),
  CONSTRAINT FK1_cases_users FOREIGN KEY (userguid) REFERENCES users ([guid]),
  CONSTRAINT FK2_cases_users FOREIGN KEY (assigneduserguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Cases_CaseStatuses')
   DROP INDEX cases.FK1_Cases_CaseStatuses
;
CREATE INDEX FK1_Cases_CaseStatuses
	 ON cases(caseStatusId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_cases_users')
   DROP INDEX cases.FK1_cases_users
;
CREATE INDEX FK1_cases_users
	 ON cases(userguid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK2_cases_users')
   DROP INDEX cases.FK2_cases_users
;
CREATE INDEX FK2_cases_users
	 ON cases(assigneduserguid);

CREATE TABLE documents (
  documentid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) NOT NULL,
  [filename] nvarchar(255) NOT NULL,
  filesizeK bigint NOT NULL default 0,
  [guid] varbinary(16) default NULL,
  updated datetime2 NOT NULL default '0001-01-01 00:00:00',
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (documentid),
  CONSTRAINT FK1_documents_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_documents_users')
   DROP INDEX documents.FK1_documents_users
;
CREATE INDEX FK1_documents_users
	 ON documents(userguid);

CREATE TABLE casedocuments (
  casedocumentid bigint NOT NULL IDENTITY(1,1),
  documentid bigint default NULL,
  caseid bigint default NULL,
  PRIMARY KEY  (casedocumentid),
  CONSTRAINT FK1_casedocuments_Case FOREIGN KEY (caseid) REFERENCES cases (caseId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_casedocuments_Document FOREIGN KEY (documentid) REFERENCES documents (documentid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_casedocuments_Case')
   DROP INDEX casedocuments.FK1_casedocuments_Case
;
CREATE INDEX FK1_casedocuments_Case
	 ON casedocuments(caseid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_casedocuments_Document')
   DROP INDEX casedocuments.FK1_casedocuments_Document
;
CREATE INDEX FK1_casedocuments_Document
	 ON casedocuments(documentid);

CREATE TABLE eventsindex (
  eventId bigint NOT NULL IDENTITY(1,1),
  channelid bigint default NULL,
  beginTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  beginMilliseconds smallint NOT NULL default 0,
  endTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endMilliseconds smallint NOT NULL default 0,
  eventTypeId smallint NOT NULL default 1,
  [status] bigint NOT NULL default 0,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  saved tinyint default 0,
  applianceid bigint default NULL,
  PRIMARY KEY  (eventId),
  CONSTRAINT FK1_EventsIndex_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON UPDATE CASCADE,
  --change into trigger
  CONSTRAINT FK1_EventsIndex_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) 
);

CREATE INDEX beginTime
	 ON eventsindex(beginTime);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'endTime')
   DROP INDEX eventsindex.endTime
;
CREATE INDEX endTime
	 ON eventsindex(endTime);

CREATE INDEX [guid]
	 ON eventsindex([guid]);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'datasource_time')
   DROP INDEX eventsindex.datasource_time
;
CREATE INDEX datasource_time
	 ON eventsindex(channelid,beginTime,beginMilliseconds);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'EventType_Index')
   DROP INDEX eventsindex.EventType_Index
;
CREATE INDEX EventType_Index
	 ON eventsindex(eventTypeId,beginTime,beginMilliseconds);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventsIndex_Appliances')
   DROP INDEX eventsindex.FK1_EventsIndex_Appliances
;
CREATE INDEX FK1_EventsIndex_Appliances
	 ON eventsindex(applianceid);

CREATE TABLE caseevents (
  caseEventId bigint NOT NULL IDENTITY(1,1),
  caseId bigint NOT NULL default 0,
  eventId bigint NOT NULL default 0,
  PRIMARY KEY  (caseEventId),
  CONSTRAINT FK1_CaseEvents_Cases FOREIGN KEY (caseId) REFERENCES cases (caseId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_CaseEvents_Events FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_CaseEvents_Cases')
   DROP INDEX caseevents.FK1_CaseEvents_Cases
;
CREATE INDEX FK1_CaseEvents_Cases
	 ON caseevents(caseId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_CaseEvents_Events')
   DROP INDEX caseevents.FK1_CaseEvents_Events
;
CREATE INDEX FK1_CaseEvents_Events
	 ON caseevents(eventId);

CREATE TABLE frames (
  frameId bigint NOT NULL IDENTITY(1,1),
  [time] datetime2 NOT NULL default '0001-01-01 00:00:00',
  milliseconds smallint NOT NULL default 0,
  [filename] nvarchar(128) NOT NULL default '',
  deviceId bigint NOT NULL default 0,
  saved tinyint NOT NULL default 0,
  width smallint NOT NULL default 0,
  height smallint NOT NULL default 0,
  pixelAspectRatioNominal tinyint NOT NULL default 1,
  pixelAspectRatioAdjustment float NOT NULL default 1.1,
  [format] tinyint NOT NULL default 6,
  fileSize bigint NOT NULL default 0,
  PRIMARY KEY  (frameId),
  CONSTRAINT FK1_Frames_Devices FOREIGN KEY (deviceId) REFERENCES devices (deviceId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'time')
   DROP INDEX frames.[time]
;
CREATE INDEX [time]
	 ON frames([time]);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'deviceid')
   DROP INDEX frames.deviceid
;
CREATE INDEX deviceid
	 ON frames(deviceId);

CREATE TABLE groups (
  groupId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  [description] nvarchar(128) default NULL,
  isSystemGroup tinyint NOT NULL default 0,
  screenOrder tinyint NOT NULL default 0,
  autoMatchType tinyint NOT NULL default 0,
  PRIMARY KEY  (groupId),
   CONSTRAINT name UNIQUE (name),
);

CREATE TABLE profiles (
  profileId bigint NOT NULL IDENTITY(1,1),
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  quality float(53) default NULL,
  width smallint NOT NULL default 0,
  height smallint NOT NULL default 0,
  pixelAspectRatioNominal tinyint NOT NULL default 1,
  pixelAspectRatioAdjustment float(24) NOT NULL default 1.10,
  [format] tinyint NOT NULL default 6,
  fileSize bigint NOT NULL default 0,
  searchData varbinary(max) NOT NULL,
  PRIMARY KEY  (profileId)
);

CREATE INDEX [guid]
	 ON profiles([guid]);

CREATE TABLE profilegroups (
  profileGroupId bigint NOT NULL IDENTITY(1,1),
  creationDate datetime2 NOT NULL default '0001-01-01 00:00:00',
  creationChannelid bigint default NULL,
  firVersion bigint default NULL,
  isAutoMatch tinyint NOT NULL default 0,
  canonicalProfileId bigint default NULL,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  host nvarchar(36) default NULL,
  PRIMARY KEY  (profileGroupId),
  --delete the both delete cascade,and add this functionality into instead of triggers of channels and profiles
  CONSTRAINT FK1_ProfileGroups_Channels FOREIGN KEY (creationChannelid) REFERENCES channels (channelid),
  CONSTRAINT FK1_ProfileGroups_Profiles FOREIGN KEY (canonicalProfileId) REFERENCES profiles (profileId) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ProfileGroups_Profiles')
   DROP INDEX profilegroups.FK1_ProfileGroups_Profiles
;
CREATE INDEX FK1_ProfileGroups_Profiles
	 ON profilegroups(canonicalProfileId);

CREATE INDEX [guid]
	 ON profilegroups([guid]);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'firversion_profilegroup')
   DROP INDEX profilegroups.firversion_profilegroup
;
CREATE INDEX firversion_profilegroup
	 ON profilegroups(firVersion,profileGroupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'Index_IsAutoMatch')
   DROP INDEX profilegroups.Index_IsAutoMatch
;
CREATE INDEX Index_IsAutoMatch
	 ON profilegroups(isAutoMatch);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ProfileGroups_Channels')
   DROP INDEX profilegroups.FK1_ProfileGroups_Channels
;
CREATE INDEX FK1_ProfileGroups_Channels
	 ON profilegroups(creationChannelid);

CREATE TABLE persons (
  personId bigint NOT NULL IDENTITY(1,1),
  firstName nvarchar(50) NOT NULL default '',
  lastName nvarchar(50) NOT NULL default '',
  lastSeen datetime2 NOT NULL default '0001-01-01 00:00:00',
  lastSeenChannelid bigint default NULL,
  firstSeen datetime2 NOT NULL default '0001-01-01 00:00:00',
  firstSeenChannelid bigint default NULL,
  groupId bigint NOT NULL default 0,
  isAutoMatch tinyint NOT NULL default 0,
  canonicalProfileGroupId bigint default NULL,
  canonicalpersonframeid bigint default NULL,
  PRIMARY KEY  (personId),
  --change to trigger
 CONSTRAINT FK1_Persons_Channels FOREIGN KEY (firstSeenChannelid) REFERENCES channels (channelid) ,
  CONSTRAINT FK1_persons_frames FOREIGN KEY (canonicalpersonframeid) REFERENCES frames (frameId) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT FK1_Persons_Groups FOREIGN KEY (groupId) REFERENCES groups (groupId) ON DELETE CASCADE ON UPDATE CASCADE,
 -- change TO trigger
  CONSTRAINT FK1_Persons_ProfileGroups FOREIGN KEY (canonicalProfileGroupId) REFERENCES profilegroups (profileGroupId) ,
  CONSTRAINT FK2_Persons_Channels FOREIGN KEY (lastSeenChannelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Persons_ProfileGroups')
   DROP INDEX persons.FK1_Persons_ProfileGroups
;
CREATE INDEX FK1_Persons_ProfileGroups
	 ON persons(canonicalProfileGroupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Persons_Groups')
   DROP INDEX persons.FK1_Persons_Groups
; 
CREATE INDEX FK1_Persons_Groups
	 ON persons(groupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Persons_Channels')
   DROP INDEX persons.FK1_Persons_Channels
;
CREATE INDEX FK1_Persons_Channels
	 ON persons(firstSeenChannelid)

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK2_Persons_Channels')
   DROP INDEX persons.FK2_Persons_Channels
;
CREATE INDEX FK2_Persons_Channels
	 ON persons(lastSeenChannelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_persons_frames')
   DROP INDEX persons.FK1_persons_frames
;
CREATE INDEX FK1_persons_frames
	 ON persons(canonicalpersonframeid);

CREATE TABLE casepersons (
  casePersonId bigint NOT NULL IDENTITY(1,1),
  caseId bigint NOT NULL default 0,
  personId bigint NOT NULL default 0,
  PRIMARY KEY  (casePersonId),
  CONSTRAINT FK1_CasePersons_Cases FOREIGN KEY (caseId) REFERENCES cases (caseId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_CasePersons_Persons FOREIGN KEY (personId) REFERENCES persons (personId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_CasePersons_Cases')
   DROP INDEX casepersons.FK1_CasePersons_Cases
;
CREATE INDEX FK1_CasePersons_Cases
	 ON casepersons(caseId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_CasePersons_Persons')
   DROP INDEX casepersons.FK1_CasePersons_Persons
;
CREATE INDEX FK1_CasePersons_Persons
	 ON casepersons(personId);

CREATE TABLE categories (
  categoryid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(128) NOT NULL,
  [readonly] tinyint NOT NULL default 0,
  syncid bigint NOT NULL default 0,
  PRIMARY KEY  (categoryid)
);

CREATE TABLE channelgroups (
  channelgroupid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  [description] nvarchar(128) default NULL,
  defaultGroup tinyint NOT NULL default 0,
  applianceid bigint NOT NULL default 1,
  PRIMARY KEY  (channelgroupid),
  CONSTRAINT uniquename UNIQUE (name,applianceid),
  CONSTRAINT FK1_channelgroups_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_channelgroups_Appliances')
   DROP INDEX channelgroups.FK1_channelgroups_Appliances
;
CREATE INDEX FK1_channelgroups_Appliances
	 ON channelgroups(applianceid);

CREATE TABLE channelgroupchannels (
  channelgroupchannelid bigint NOT NULL IDENTITY(1,1),
  channelid bigint default NULL,
  channelgroupid bigint default NULL,
  PRIMARY KEY  (channelgroupchannelid),
  CONSTRAINT FK1_ChannelGroupchannels_ChannelGroups FOREIGN KEY (channelgroupid) REFERENCES channelgroups (channelgroupid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_ChannelGroupChannels_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelGroupChannels_Channels')
   DROP INDEX channelgroupchannels.FK1_ChannelGroupChannels_Channels
;
CREATE INDEX FK1_ChannelGroupChannels_Channels
	 ON channelgroupchannels(channelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelGroupchannels_ChannelGroups')
   DROP INDEX channelgroupchannels.FK1_ChannelGroupchannels_ChannelGroups
;
CREATE INDEX FK1_ChannelGroupchannels_ChannelGroups
	 ON channelgroupchannels(channelgroupid);

CREATE TABLE channelmetadataelements (
  channelMetadataElementId bigint NOT NULL IDENTITY(1,1),
  channelId bigint default NULL,
  metadataElementId bigint default NULL,
  PRIMARY KEY  (channelMetadataElementId),
  CONSTRAINT FK1_ChannelMetadataElements_Channels FOREIGN KEY (channelId) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_ChannelMetadataElements_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelMetadataElements_Channels')
   DROP INDEX channelmetadataelements.FK1_ChannelMetadataElements_Channels
;
CREATE INDEX FK1_ChannelMetadataElements_Channels
	 ON channelmetadataelements(channelId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelMetadataElements_MetadataElements')
   DROP INDEX channelmetadataelements.FK1_ChannelMetadataElements_MetadataElements
;
CREATE INDEX FK1_ChannelMetadataElements_MetadataElements
	 ON channelmetadataelements(metadataElementId);

CREATE TABLE channelptzpresets (
  channelPtzPresetId bigint NOT NULL IDENTITY(1,1),
  channelid bigint NOT NULL default 0,
  name nvarchar(64) NOT NULL default '',
  theta float(53) NOT NULL default 0,
  phi  float(53) NOT NULL default 0,
  zoom  float(53) NOT NULL default 0,
  iris  float(53) NOT NULL default 0,
  focus  float(53) NOT NULL default 0,
  autoFocus tinyint NOT NULL default 1,
  autoIris tinyint NOT NULL default 1,
  ishome tinyint NOT NULL default 0,
  PRIMARY KEY  (channelPtzPresetId),
  CONSTRAINT FK1_ptzPresets_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ptzPresets_Channels')
   DROP INDEX channelptzpresets.FK1_ptzPresets_Channels
;
CREATE INDEX FK1_ptzPresets_Channels
	 ON channelptzpresets(channelid);

CREATE TABLE channelresolutioninformation (
  channelResolutionInformationId bigint NOT NULL IDENTITY(1,1),
  width bigint default NULL,
  height bigint default NULL,
  gridWidth bigint default NULL,
  gridHeight bigint default NULL,
  aspectRatioNominal bigint default NULL,
  aspectRatioAdjustment float(53) default NULL,
  channelid bigint NOT NULL default 0,
  PRIMARY KEY  (channelResolutionInformationId),
  CONSTRAINT FK1_ChannelResolutionInformation_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelResolutionInformation_Channels')
   DROP INDEX channelresolutioninformation.FK1_ChannelResolutionInformation_Channels
;
CREATE INDEX FK1_ChannelResolutionInformation_Channels
	 ON channelresolutioninformation(channelid);

CREATE TABLE channelsettings (
  channelSettingId bigint NOT NULL IDENTITY(1,1),
  channelType int NOT NULL default 0,
  settingId bigint default NULL,
  PRIMARY KEY  (channelSettingId),
  CONSTRAINT FK1_ChannelSettings_Settings FOREIGN KEY (settingId) REFERENCES settings (settingId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ChannelSettings_Settings')
   DROP INDEX channelsettings.FK1_ChannelSettings_Settings
;
CREATE INDEX FK1_ChannelSettings_Settings
	 ON channelsettings(settingId);

CREATE TABLE configvalues (
  configvalueid bigint NOT NULL IDENTITY(1,1),
  userid bigint default NULL,
  created datetime2 default NULL,
  configkey nvarchar(255) default NULL,
  configvalue nvarchar(255) default NULL,
  PRIMARY KEY  (configvalueid)
);

CREATE TABLE counters (
  counterid bigint NOT NULL IDENTITY(1,1),
  applianceid bigint NOT NULL default 0,
  channelid bigint NOT NULL default 0,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  name nvarchar(128) NOT NULL default '',
  isenabled tinyint NOT NULL default 1,
  direction tinyint NOT NULL default 0,
  shape nvarchar(36) NOT NULL default 'line',
  categoryid bigint default NULL,
  PRIMARY KEY  (counterid),
  CONSTRAINT FK1_peopleCounters_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT fk_countercategory FOREIGN KEY (categoryid) REFERENCES categories (categoryid) ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_peopleCounters_Channels')
   DROP INDEX counters.FK1_peopleCounters_Channels
;
CREATE INDEX FK1_peopleCounters_Channels
	 ON counters(channelid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'fk_countercategory')
   DROP INDEX counters.fk_countercategory
;
CREATE INDEX fk_countercategory
	 ON counters(categoryid);

CREATE TABLE countercrossings (
  CounterCrossingid bigint NOT NULL IDENTITY(1,1),
  counterid bigint NOT NULL default 0,
  direction bigint NOT NULL default 0,
  crossTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  crossMilliseconds smallint NOT NULL default 0,
  distance float NOT NULL default 0,
  framestracked bigint default 0,
  flowvectors bigint default 0,
  xvelocity float default 0,
  yvelocity float default 0,
  totalarea float default 0,
  objectsassociated bigint default 0,
  PRIMARY KEY  (CounterCrossingid),
  CONSTRAINT FK1_CounterCrossings_Counters FOREIGN KEY (counterid) REFERENCES counters (counterid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_CounterCrossings_Counters')
   DROP INDEX countercrossings.FK1_CounterCrossings_Counters
;
CREATE INDEX FK1_CounterCrossings_Counters
	 ON countercrossings(counterid);

CREATE TABLE counterpoints (
  counterpointid bigint NOT NULL IDENTITY(1,1),
  counterid bigint NOT NULL default 0,
  locationX float NOT NULL default 0,
  locationY float NOT NULL default 0,
  pointorder bigint NOT NULL default 0,
  PRIMARY KEY  (counterpointid),
  CONSTRAINT FK1_Counterpoints_Counters FOREIGN KEY (counterid) REFERENCES counters (counterid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Counterpoints_Counters')
   DROP INDEX counterpoints.FK1_Counterpoints_Counters
;
CREATE INDEX FK1_Counterpoints_Counters
	 ON counterpoints(counterid);

CREATE TABLE edgenodes (
  edgenodeId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) default NULL,
  hostAddress nvarchar(255) default NULL,
  machineType int default NULL,
  license nvarchar(255) default NULL,
  [status] bigint default NULL,
  requestedStatus bigint NOT NULL default 0,
  installdate datetime2 NOT NULL default '0001-01-01 00:00:00',
  PRIMARY KEY  (edgenodeId)
);

CREATE TABLE emailrecipients (
  emailRecipientId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  sendTo nvarchar(128) default NULL,
  PRIMARY KEY  (emailRecipientId)
);

CREATE TABLE emailactions (
  emailActionId bigint NOT NULL IDENTITY(1,1),
  emailId bigint NOT NULL default 0,
  emailRecipientId bigint NOT NULL default 0,
  PRIMARY KEY  (emailActionId),
  CONSTRAINT FK1_EmailActions_EmailRecipients FOREIGN KEY (emailRecipientId) REFERENCES emailrecipients (emailRecipientId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_EmailActions_Emails FOREIGN KEY (emailId) REFERENCES emails (emailId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EmailActions_Emails')
   DROP INDEX emailactions.FK1_EmailActions_Emails
;
CREATE INDEX FK1_EmailActions_Emails
	 ON emailactions(emailId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EmailActions_EmailRecipients')
   DROP INDEX emailactions.FK1_EmailActions_EmailRecipients
;
CREATE INDEX FK1_EmailActions_EmailRecipients
	 ON emailactions(emailRecipientId);

CREATE TABLE enumeratedtypevalues (
  enumeratedTypeValueId bigint NOT NULL IDENTITY(1,1),
  metadataKeyId bigint default NULL,
  value nvarchar(255) default NULL,
  PRIMARY KEY  (enumeratedTypeValueId),
  CONSTRAINT FK1_MetadataEnumeratedValues_MetadataKeys FOREIGN KEY (metadataKeyId) REFERENCES metadatakeys (metadataKeyId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataEnumeratedValues_MetadataKeys')
   DROP INDEX enumeratedtypevalues.FK1_MetadataEnumeratedValues_MetadataKeys
;
CREATE INDEX FK1_MetadataEnumeratedValues_MetadataKeys
	 ON enumeratedtypevalues(metadataKeyId);

CREATE TABLE eventalerts (
  eventAlertId bigint NOT NULL IDENTITY(1,1),
  eventId bigint NOT NULL default 0,
  alertId bigint NOT NULL default 0,
  triggerdata nvarchar(max),
  PRIMARY KEY  (eventAlertId),
   CONSTRAINT eventalerts_eventid_alertid_unique UNIQUE (eventId,alertId),
  CONSTRAINT FK1_EventAlerts_Alerts FOREIGN KEY (alertId) REFERENCES alerts (alertId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_EventAlerts_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventAlerts_EventsIndex')
   DROP INDEX eventalerts.FK1_EventAlerts_EventsIndex
;
CREATE INDEX FK1_EventAlerts_EventsIndex
	 ON eventalerts(eventId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventAlerts_Alerts')
   DROP INDEX eventalerts.FK1_EventAlerts_Alerts
;
CREATE INDEX FK1_EventAlerts_Alerts
	 ON eventalerts(alertId);

CREATE TABLE eventcleanupinfo (
  eventcleanupinfoid bigint NOT NULL default 0,
  [time] datetime2 NOT NULL default '0001-01-01 00:00:00',
  profileid bigint NOT NULL default 0,
  PRIMARY KEY  (eventcleanupinfoid)
);

CREATE TABLE eventgrids (
  eventGridId bigint NOT NULL IDENTITY(1,1),
  eventId bigint NOT NULL default 0,
  direction tinyint NOT NULL default 0,
  ULx bigint NOT NULL default 0,
  ULy bigint NOT NULL default 0,
  LRx bigint NOT NULL default 0,
  LRy bigint NOT NULL default 0,
  PRIMARY KEY  (eventGridId),
  CONSTRAINT FK1_EventGrids_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'direction')
   DROP INDEX eventgrids.direction
;
CREATE INDEX direction
	 ON eventgrids(direction);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'ULx')
   DROP INDEX eventgrids.ULx
;
CREATE INDEX ULx
	 ON eventgrids(ULx);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'ULy')
   DROP INDEX eventgrids.ULy
;
CREATE INDEX ULy
	 ON eventgrids(ULy);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'LRx')
   DROP INDEX eventgrids.LRx
;
CREATE INDEX LRx
	 ON eventgrids(LRx);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'LRy')
   DROP INDEX eventgrids.LRy
;
CREATE INDEX LRy
	 ON eventgrids(LRy);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventGrids_EventsIndex')
   DROP INDEX eventgrids.FK1_EventGrids_EventsIndex
;
CREATE INDEX FK1_EventGrids_EventsIndex
	 ON eventgrids(eventId);

CREATE TABLE eventmetadataelements (
  eventMetadataElementId bigint NOT NULL IDENTITY(1,1),
  eventId bigint default NULL,
  metadataElementId bigint default NULL,
  PRIMARY KEY  (eventMetadataElementId),
  CONSTRAINT FK1_EventMetadataElements_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_EventMetadataElements_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventMetadataElements_MetadataElements')
   DROP INDEX eventmetadataelements.FK1_EventMetadataElements_MetadataElements
;
CREATE INDEX FK1_EventMetadataElements_MetadataElements
	 ON eventmetadataelements(metadataElementId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_EventMetadataElements_EventsIndex')
   DROP INDEX eventmetadataelements.FK1_EventMetadataElements_EventsIndex
;
CREATE INDEX FK1_EventMetadataElements_EventsIndex
	 ON eventmetadataelements(eventId);

CREATE TABLE eventsearchplugindata (
  eventsearchplugindataId bigint NOT NULL IDENTITY(1,1),
  searchpluginguid varbinary(16) default NULL,
  eventId bigint NOT NULL default 0,
  searchPluginData varbinary(max),
  PRIMARY KEY  (eventsearchplugindataId),
  CONSTRAINT FK1_eventsearchplugindata_Events FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_eventsearchplugindata_Events')
   DROP INDEX eventsearchplugindata.FK1_eventsearchplugindata_Events
;
CREATE INDEX FK1_eventsearchplugindata_Events
	 ON eventsearchplugindata(eventId);

CREATE TABLE genericeventdefinitions (
  genericEventDefinitionId bigint NOT NULL IDENTITY(1,1),
  displayName nvarchar(50) NOT NULL default '',
  shortDisplayName nvarchar(20) NOT NULL default '',
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  templateType tinyint NOT NULL default 0,
  helpText nvarchar(max),
  istransaction tinyint NOT NULL default 0,
  numbercardimages bigint NOT NULL default 1,
  PRIMARY KEY  (genericEventDefinitionId),
   CONSTRAINT genericeventdefinitions_guid UNIQUE ([guid]),
);

CREATE TABLE eventtypetrimmingschedules (
  eventtypetrimmingscheduleid bigint NOT NULL IDENTITY(1,1),
  eventTypeId smallint NOT NULL default 1,
  genericEventDefinitionId bigint default NULL,
  daysbeforedataexpiration smallint default NULL,
  lastdatatime datetime2 default '1970-01-01 00:00:00',
  lastframegc datetime2 default '1970-01-01 00:00:00',
  PRIMARY KEY  (eventtypetrimmingscheduleid),
  CONSTRAINT FK1_eventtypetrimmingscheduleGEDs_GEDs FOREIGN KEY (genericEventDefinitionId) REFERENCES genericeventdefinitions (genericEventDefinitionId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_eventtypetrimmingscheduleGEDs_GEDs')
   DROP INDEX eventtypetrimmingschedules.FK1_eventtypetrimmingscheduleGEDs_GEDs
;
CREATE INDEX FK1_eventtypetrimmingscheduleGEDs_GEDs
	 ON eventtypetrimmingschedules(genericEventDefinitionId);

CREATE TABLE faceevents (
  faceEventId bigint NOT NULL IDENTITY(1,1),
  faceEventType tinyint NOT NULL default 0,
  eventId bigint NOT NULL default 0,
  profileId bigint default NULL,
  matchType tinyint NOT NULL default 0,
  profileGroupId bigint default NULL,
  matchedByUserId bigint default NULL,
  PRIMARY KEY  (faceEventId),
  CONSTRAINT FK1_FaceEvents_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE,
  --change to trigger
  CONSTRAINT FK1_FaceEvents_ProfileGroups FOREIGN KEY (profileGroupId) REFERENCES profilegroups (profileGroupId) ,
  CONSTRAINT FK1_FaceEvents_Profiles FOREIGN KEY (profileId) REFERENCES profiles (profileId) ON DELETE SET NULL ON UPDATE CASCADE,
 --There are no primary or candidate keys in the referenced table 'users' that match the referencing column list in the foreign key 'FK1_FaceEvents_Users'.
 --because userId is not primary key,and also not unique constraint,so we can't create the following FOREIGN KEY.
 --and because the userId can be null in users table,so if we have one record that the userId is null in users table,the following FOREIGN KEY can't work 
 -- CONSTRAINT FK1_FaceEvents_Users FOREIGN KEY (matchedByUserId) REFERENCES users (userId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEvents_EventsIndex')
   DROP INDEX faceevents.FK1_FaceEvents_EventsIndex
;
CREATE NONCLUSTERED INDEX FK1_FaceEvents_EventsIndex
	 ON faceevents(eventId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEvents_ProfileGroups')
   DROP INDEX faceevents.FK1_FaceEvents_ProfileGroups
;
CREATE INDEX FK1_FaceEvents_ProfileGroups
	 ON faceevents(profileGroupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEvents_Users')
   DROP INDEX faceevents.FK1_FaceEvents_Users
;
CREATE INDEX FK1_FaceEvents_Users
	 ON faceevents(matchedByUserId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'faceEventType')
   DROP INDEX faceevents.faceEventType
;
CREATE INDEX faceEventType
	 ON faceevents(faceEventType);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEvents_Profiles')
   DROP INDEX faceevents.FK1_FaceEvents_Profiles
;
CREATE INDEX FK1_FaceEvents_Profiles
	 ON faceevents(profileId);

CREATE TABLE framefacedata (
  frameFaceDataId bigint NOT NULL IDENTITY(1,1),
  frameId bigint NOT NULL default 0,
  faceLocationX float(53) NOT NULL default 0,
  faceLocationY float(53) NOT NULL default 0,
  faceWidth float(53) NOT NULL default 0,
  faceQuality float(53) NOT NULL default 0,
  faceConfidence float(53) default NULL,
  eyeLocationFirstX float(53) default NULL,
  eyeLocationFirstY float(53) default NULL,
  eyeLocationSecondX float(53) default NULL,
  eyeLocationSecondY float(53) default NULL,
  eyeFirstConfidence float(53) default NULL,
  eyeSecondConfidence float(53) default NULL,
  imageQuality float(53) default NULL,
  brightnessRating bigint default NULL,
  brightnessScore float(53) default NULL,
  hasGlassesDecision tinyint default NULL,
  hasGlassesScore float(53) default NULL,
  subgroupFrame tinyint default NULL,
  PRIMARY KEY  (frameFaceDataId),
  CONSTRAINT FK1_FrameFaceData_Frames FOREIGN KEY (frameId) REFERENCES frames (frameId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FrameFaceData_Frames')
   DROP INDEX framefacedata.FK1_FrameFaceData_Frames
;
CREATE INDEX FK1_FrameFaceData_Frames
	 ON framefacedata(frameId);

CREATE TABLE faceeventframedata (
  faceEventFrameDataId bigint NOT NULL IDENTITY(1,1),
  faceEventId bigint NOT NULL default 0,
  frameFaceDataId bigint NOT NULL default 0,
  PRIMARY KEY  (faceEventFrameDataId),
  CONSTRAINT FK1_FaceEventFrameData_FaceEvents FOREIGN KEY (faceEventId) REFERENCES faceevents (faceEventId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_FaceEventFrameData_FrameFaceData FOREIGN KEY (frameFaceDataId) REFERENCES framefacedata (frameFaceDataId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEventFrameData_FaceEvents')
   DROP INDEX faceeventframedata.FK1_FaceEventFrameData_FaceEvents
;
CREATE INDEX FK1_FaceEventFrameData_FaceEvents
	 ON faceeventframedata(faceEventId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEventFrameData_FrameFaceData')
   DROP INDEX faceeventframedata.FK1_FaceEventFrameData_FrameFaceData
;
CREATE INDEX FK1_FaceEventFrameData_FrameFaceData
	 ON faceeventframedata(frameFaceDataId);

CREATE TABLE importedimagesets (
  importedImageSetId bigint NOT NULL IDENTITY(1,1),
  importTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  name nvarchar(50) default NULL,
  PRIMARY KEY  (importedImageSetId)
);

CREATE TABLE importedimageevents (
  importedImageEventId bigint NOT NULL IDENTITY(1,1),
  eventId bigint NOT NULL default 0,
  imageLastModified datetime2 NOT NULL default '0001-01-01 00:00:00',
  sourcePath nvarchar(120) default NULL,
  [filename] nvarchar(50) default NULL,
  importedImageSetId bigint NOT NULL default 0,
  frameId bigint default NULL,
  MD5Checksum nvarchar(100) default NULL,
  hasDuplicates tinyint NOT NULL default 0,
  PRIMARY KEY  (importedImageEventId),
  --change into trigger
  CONSTRAINT FK1_ImportedImageEvents_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ,
  CONSTRAINT FK1_ImportedImageEvents_ImportedImageSets FOREIGN KEY (importedImageSetId) REFERENCES importedimagesets (importedImageSetId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ImportedImageEvents_ImportedImageSets')
   DROP INDEX importedimageevents.FK1_ImportedImageEvents_ImportedImageSets
;
CREATE INDEX FK1_ImportedImageEvents_ImportedImageSets
	 ON importedimageevents(importedImageSetId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ImportedImageEvents_EventsIndex')
   DROP INDEX importedimageevents.FK1_ImportedImageEvents_EventsIndex
;
CREATE INDEX FK1_ImportedImageEvents_EventsIndex
	 ON importedimageevents(eventId);

CREATE TABLE faceeventimportedimageevents (
  faceEventImportedImageEventId bigint NOT NULL IDENTITY(1,1),
  faceEventId bigint NOT NULL default 0,
  importedImageEventId bigint NOT NULL default 0,
  PRIMARY KEY  (faceEventImportedImageEventId),
  CONSTRAINT FK1_FaceEventImportedImageEvents_FaceEvents FOREIGN KEY (faceEventId) REFERENCES faceevents (faceEventId) ON DELETE CASCADE ON UPDATE CASCADE,
--change to trigger
  CONSTRAINT FK1_FaceEventImportedImageEvents_ImportedImageEvents FOREIGN KEY (importedImageEventId) REFERENCES importedimageevents (importedImageEventId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEventImportedImageEvents_FaceEvents')
   DROP INDEX faceeventimportedimageevents.FK1_FaceEventImportedImageEvents_FaceEvents
;
CREATE INDEX FK1_FaceEventImportedImageEvents_FaceEvents
	 ON faceeventimportedimageevents(faceEventId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceEventImportedImageEvents_ImportedImageEvents')
   DROP INDEX faceeventimportedimageevents.FK1_FaceEventImportedImageEvents_ImportedImageEvents
;
CREATE INDEX FK1_FaceEventImportedImageEvents_ImportedImageEvents
	 ON faceeventimportedimageevents(importedImageEventId);

CREATE TABLE facerecupgrades (
  faceRecUpgradeId bigint NOT NULL IDENTITY(1,1),
  newFirVersion bigint NOT NULL default 0,
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endTime datetime2 default NULL,
  profilesToUpgrade bigint default NULL,
  PRIMARY KEY  (faceRecUpgradeId)
);

CREATE TABLE facerecupgradeprofilecheck (
  facerecupgradeprofilecheckId bigint NOT NULL IDENTITY(1,1),
  faceRecUpgradeId bigint NOT NULL default 0,
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endTime datetime2 default NULL,
  currentprofile bigint NOT NULL default 0,
  lastprofile bigint NOT NULL default 0,
  PRIMARY KEY  (facerecupgradeprofilecheckId),
  CONSTRAINT FK1_FaceRecUpgradeProfileCheck_FaceRecUpgrades FOREIGN KEY (faceRecUpgradeId) REFERENCES facerecupgrades (faceRecUpgradeId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceRecUpgradeProfileCheck_FaceRecUpgrades')
   DROP INDEX facerecupgradeprofilecheck.FK1_FaceRecUpgradeProfileCheck_FaceRecUpgrades
;
CREATE INDEX FK1_FaceRecUpgradeProfileCheck_FaceRecUpgrades
	 ON facerecupgradeprofilecheck(faceRecUpgradeId);

CREATE TABLE facerecupgradeprofilecheckstats (
  facerecupgradeprofilecheckstatId bigint NOT NULL IDENTITY(1,1),
  facerecupgradeprofilecheckId bigint NOT NULL default 0,
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  lastUpdateTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  profileschecked bigint NOT NULL default 0,
  profilesdeleted bigint NOT NULL default 0,
  PRIMARY KEY  (facerecupgradeprofilecheckstatId),
  CONSTRAINT FK1_FaceRecUpgradeProfileCheckStats_FaceRecUpgradeProfileCheck FOREIGN KEY (facerecupgradeprofilecheckId) REFERENCES facerecupgradeprofilecheck (facerecupgradeprofilecheckId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceRecUpgradeProfileCheckStats_FaceRecUpgradeProfileCheck')
   DROP INDEX facerecupgradeprofilecheckstats.FK1_FaceRecUpgradeProfileCheckStats_FaceRecUpgradeProfileCheck
;
CREATE INDEX FK1_FaceRecUpgradeProfileCheckStats_FaceRecUpgradeProfileCheck
	 ON facerecupgradeprofilecheckstats(facerecupgradeprofilecheckId);

CREATE TABLE facerecupgradestats (
  faceRecUpgradeStatId bigint NOT NULL IDENTITY(1,1),
  faceRecUpgradeId bigint NOT NULL default 0,
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  lastUpdateTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  profilesProcessed bigint NOT NULL default 0,
  usedDbFaceInfo bigint NOT NULL default 0,
  ranFaceEyeFind bigint NOT NULL default 0,
  profilesUpgraded bigint NOT NULL default 0,
  profilesDeleted bigint NOT NULL default 0,
  failedFaceFind bigint NOT NULL default 0,
  failedEyeFind bigint NOT NULL default 0,
  failedFaceDistanceCheck bigint NOT NULL default 0,
  failedFirFilter bigint NOT NULL default 0,
  profileGroupsUpdated bigint NOT NULL default 0,
  profileGroupsDeleted bigint NOT NULL default 0,
  personsDeleted bigint NOT NULL default 0,
  PRIMARY KEY  (faceRecUpgradeStatId),
  CONSTRAINT FK1_FaceRecUpgradeStats_FaceRecUpgrades FOREIGN KEY (faceRecUpgradeId) REFERENCES facerecupgrades (faceRecUpgradeId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FaceRecUpgradeStats_FaceRecUpgrades')
   DROP INDEX facerecupgradestats.FK1_FaceRecUpgradeStats_FaceRecUpgrades
;
CREATE INDEX FK1_FaceRecUpgradeStats_FaceRecUpgrades
	 ON facerecupgradestats(faceRecUpgradeId);

CREATE TABLE fileinputjobs (
  fileInputJobId bigint NOT NULL IDENTITY(1,1),
  jobName nvarchar(80) default NULL,
  channelid bigint NOT NULL default 0,
  completionState smallint NOT NULL default 0,
  errorCode smallint NOT NULL default 0,
  cancelRequested tinyint default 0,
  serializedMetadata varbinary(max),
  MD5Checksum nvarchar(100) default NULL,
  filePath nvarchar(100) default NULL,
  PRIMARY KEY  (fileInputJobId),
  CONSTRAINT FK1_FileInputJobs_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FileInputJobs_Channels')
   DROP INDEX fileinputjobs.FK1_FileInputJobs_Channels
;
CREATE INDEX FK1_FileInputJobs_Channels
	 ON fileinputjobs(channelid);

CREATE TABLE filesystemmetrics (
  filesystemmetricid bigint NOT NULL IDENTITY(1,1),
  [type] smallint NOT NULL default 0,
  [date] date NOT NULL default '0000-00-00',
  byteswritten bigint NOT NULL default 0,
  PRIMARY KEY  (filesystemmetricid)
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'date')
   DROP INDEX filesystemmetrics.[date]
;
CREATE INDEX [date]
	 ON filesystemmetrics([date]);

CREATE TABLE flags (
  flagId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  isSystemFlag tinyint default 0,
  objectType tinyint default 0,
  PRIMARY KEY  (flagId)
);

CREATE TABLE motionevents (
  motionEventId bigint NOT NULL IDENTITY(1,1),
  eventId bigint NOT NULL default 0,
  gridSizeX bigint NOT NULL default 0,
  gridSizeY bigint NOT NULL default 0,
  pixelAspectRatioNominal tinyint NOT NULL default 1,
  pixelAspectRatioAdjustment float NOT NULL default 1.1,
  cumulativeMotionPresenceData varbinary(max),
  cumulativeMotionDirectionData varbinary(max),
  PRIMARY KEY  (motionEventId),
  --change into trigger
  CONSTRAINT FK1_MotionEvents_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId)
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MotionEvents_EventsIndex')
   DROP INDEX motionevents.FK1_MotionEvents_EventsIndex
;
CREATE INDEX FK1_MotionEvents_EventsIndex
	 ON motionevents(eventId);

CREATE TABLE framemotiondata (
  frameMotionDataId bigint NOT NULL IDENTITY(1,1),
  motioneventid bigint NOT NULL default 0,
  frameid bigint default NULL,
  userCreated tinyint NOT NULL default 0,
  PRIMARY KEY  (frameMotionDataId),
  CONSTRAINT FK1_FrameMotionData_Frames FOREIGN KEY (frameid) REFERENCES frames (frameId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_FrameMotionData_MotionEvents FOREIGN KEY (motioneventid) REFERENCES motionevents (motionEventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'frameid')
   DROP INDEX framemotiondata.frameid
;
CREATE INDEX frameid
	 ON framemotiondata(frameid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_FrameMotionData_MotionEvents')
   DROP INDEX framemotiondata.FK1_FrameMotionData_MotionEvents
;
CREATE INDEX FK1_FrameMotionData_MotionEvents
	 ON framemotiondata(motioneventid);

CREATE TABLE genericeventdefinitionmetadatakeys (
  genericEventDefinitionMetadataKeyId bigint NOT NULL IDENTITY(1,1),
  genericEventDefinitionId bigint default NULL,
  metadataKeyId bigint NOT NULL default 0,
  displayFormat nvarchar(128) NOT NULL default '{value}',
  PRIMARY KEY  (genericEventDefinitionMetadataKeyId),
  CONSTRAINT FK1_GenericEventDefinitionMetadataKey_GenericEventDefinitions FOREIGN KEY (genericEventDefinitionId) REFERENCES genericeventdefinitions (genericEventDefinitionId),
  CONSTRAINT FK1_GenericEventDefinitionMetadataKey_MetadataKeys FOREIGN KEY (metadataKeyId) REFERENCES metadatakeys (metadataKeyId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_GenericEventDefinitionMetadataKey_MetadataKeys')
   DROP INDEX genericeventdefinitionmetadatakeys.FK1_GenericEventDefinitionMetadataKey_MetadataKeys
;
CREATE INDEX FK1_GenericEventDefinitionMetadataKey_MetadataKeys
	 ON genericeventdefinitionmetadatakeys(metadataKeyId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_GenericEventDefinitionMetadataKey_GenericEventDefinitions')
   DROP INDEX genericeventdefinitionmetadatakeys.FK1_GenericEventDefinitionMetadataKey_GenericEventDefinitions
;
CREATE INDEX FK1_GenericEventDefinitionMetadataKey_GenericEventDefinitions
	 ON genericeventdefinitionmetadatakeys(genericEventDefinitionId);

CREATE TABLE genericevents (
  genericEventId bigint NOT NULL IDENTITY(1,1),
  genericEventDefinitionId bigint default NULL,
  eventId bigint NOT NULL default 0,
  eventtime datetime2 default NULL,
  eventMilliseconds smallint NOT NULL default 0,
  PRIMARY KEY  (genericEventId),
  CONSTRAINT FK1_GenericEvents_EventsIndex FOREIGN KEY (eventId) REFERENCES eventsindex (eventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_GenericEvents_EventsIndex')
   DROP INDEX genericevents.FK1_GenericEvents_EventsIndex
;
CREATE INDEX FK1_GenericEvents_EventsIndex
	 ON genericevents(eventId);

CREATE TABLE genericeventframedata (
  genericEventFrameDataId bigint NOT NULL IDENTITY(1,1),
  genericEventId bigint NOT NULL default 0,
  frameId bigint NOT NULL default 0,
  displayPosition tinyint NOT NULL default 0,
  displayx float NOT NULL default 0,
  displayy float NOT NULL default 0,
  displaywidth float NOT NULL default 1,
  displayheight float NOT NULL default 1,
  PRIMARY KEY  (genericEventFrameDataId),
  CONSTRAINT FK1_GenericEventFrameData_Frames FOREIGN KEY (frameId) REFERENCES frames (frameId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_GenericEventFrameData_GenericEvents FOREIGN KEY (genericEventId) REFERENCES genericevents (genericEventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_GenericEventFrameData_Frames')
   DROP INDEX genericeventframedata.FK1_GenericEventFrameData_Frames
;
CREATE INDEX FK1_GenericEventFrameData_Frames
	 ON genericeventframedata(frameId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_GenericEventFrameData_GenericEvents')
   DROP INDEX genericeventframedata.FK1_GenericEventFrameData_GenericEvents
;
CREATE INDEX FK1_GenericEventFrameData_GenericEvents
	 ON genericeventframedata(genericEventId);

CREATE TABLE healthalerttypes (
  healthAlertTypeId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) NOT NULL default '',
  displayLabel nvarchar(50) NOT NULL default '',
  severity bigint NOT NULL default 0,
  isEnabled tinyint NOT NULL default 1,
  supportsOpenClose tinyint NOT NULL default 0,
  PRIMARY KEY  (healthAlertTypeId)
);

CREATE TABLE healthalertemails (
  healthAlertEmailId bigint NOT NULL IDENTITY(1,1),
  healthAlertTypeId bigint NOT NULL default 0,
  emailRecipientId bigint default NULL,
  triggercode tinyint NOT NULL default 0,
  PRIMARY KEY  (healthAlertEmailId),
  CONSTRAINT healthalertemails_unique UNIQUE (healthAlertTypeId,emailRecipientId,triggercode),
  CONSTRAINT FK1_healthAlertEmails_EmailRecipients FOREIGN KEY (emailRecipientId) REFERENCES emailrecipients (emailRecipientId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_healthalertemails_healthAlertTypes FOREIGN KEY (healthAlertTypeId) REFERENCES healthalerttypes (healthAlertTypeId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_healthAlertEmails_EmailRecipients')
   DROP INDEX healthalertemails.FK1_healthAlertEmails_EmailRecipients
;
CREATE INDEX FK1_healthAlertEmails_EmailRecipients
	 ON healthalertemails(emailRecipientId);

CREATE TABLE healthalerts (
  healthAlertId bigint NOT NULL IDENTITY(1,1),
  healthAlertTypeId bigint NOT NULL default 0,
  beginTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  [message] nvarchar(max),
  isTriggered tinyint NOT NULL default 1,
  endTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  isOpen tinyint NOT NULL default 0,
  specifier nvarchar(255) NOT NULL default '',
  applianceid bigint NOT NULL,
  --servertype enum('Core','Edge','Enterprise') NOT NULL default 'Core',
  servertype nvarchar(50) NOT NULL CHECK (servertype IN('Core','Edge','Enterprise'))default 'Core',
  servername nvarchar(255) default NULL,
  triggerTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  [state] nvarchar(max) NOT NULL,
  PRIMARY KEY  (healthAlertId),
  CONSTRAINT FK1_healthAlerts_healthAlertTypes FOREIGN KEY (healthAlertTypeId) REFERENCES healthalerttypes (healthAlertTypeId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_Systemalerts_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_Systemalerts_Appliances')
   DROP INDEX healthalerts.FK1_Systemalerts_Appliances
;
CREATE INDEX FK1_Systemalerts_Appliances
	 ON healthalerts(applianceid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_healthAlerts_healthAlertTypes')
   DROP INDEX healthalerts.FK1_healthAlerts_healthAlertTypes
;
CREATE INDEX FK1_healthAlerts_healthAlertTypes
	 ON healthalerts(healthAlertTypeId);

CREATE TABLE importregularexpressions (
  importRegularExpressionId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  expression nvarchar(200) default NULL,
  PRIMARY KEY  (importRegularExpressionId)
);

CREATE TABLE importregularexpressiongroupingkeys (
  importRegularExpressionGroupingKeyId bigint NOT NULL IDENTITY(1,1),
  importRegularExpressionId bigint default NULL,
  groupingNumber bigint default NULL,
  metadataKeyId bigint default NULL,
  PRIMARY KEY  (importRegularExpressionGroupingKeyId),
  CONSTRAINT FK1_ImportRegularExpressionGroupingKeys_ImportRegularExpressions FOREIGN KEY (importRegularExpressionId) REFERENCES importregularexpressions (importRegularExpressionId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_ImportRegularExpressionGroupingKeys_MetadataKeys FOREIGN KEY (metadataKeyId) REFERENCES metadatakeys (metadataKeyId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ImportRegularExpressionGroupingKeys_ImportRegularExpressions')
   DROP INDEX importregularexpressiongroupingkeys.FK1_ImportRegularExpressionGroupingKeys_ImportRegularExpressions
;
CREATE INDEX FK1_ImportRegularExpressionGroupingKeys_ImportRegularExpressions
	 ON importregularexpressiongroupingkeys(importRegularExpressionId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_ImportRegularExpressionGroupingKeys_MetadataKeys')
   DROP INDEX importregularexpressiongroupingkeys.FK1_ImportRegularExpressionGroupingKeys_MetadataKeys
;
CREATE INDEX FK1_ImportRegularExpressionGroupingKeys_MetadataKeys
	 ON importregularexpressiongroupingkeys(metadataKeyId);

CREATE TABLE metadatabinaryvalues (
  metadatabinaryvalueId bigint NOT NULL IDENTITY(1,1),
  metadataElementId bigint default NULL,
  value varbinary(max) NOT NULL,
  [path] nvarchar(255) default NULL,
  mimetype tinyint NOT NULL default 0,
  PRIMARY KEY  (metadatabinaryvalueId),
  CONSTRAINT FK1_MetadataBinaryValues_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataStringValues_MetadataElements')
   DROP INDEX metadatabinaryvalues.FK1_MetadataStringValues_MetadataElements
;
CREATE INDEX FK1_MetadataStringValues_MetadataElements
	 ON metadatabinaryvalues(metadataElementId);

CREATE TABLE metadatadatevalues (
  metadataDateValueId bigint NOT NULL IDENTITY(1,1),
  metadataElementId bigint default NULL,
  value datetime2 NOT NULL default '0001-01-01 00:00:00',
  PRIMARY KEY  (metadataDateValueId),
  CONSTRAINT FK1_MetadataDateValues_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataDateValues_MetadataElements')
   DROP INDEX metadatadatevalues.FK1_MetadataDateValues_MetadataElements
;
CREATE INDEX FK1_MetadataDateValues_MetadataElements
	 ON metadatadatevalues(metadataElementId);

CREATE TABLE metadataenumeratedvalues (
  metadataEnumeratedValueId bigint NOT NULL IDENTITY(1,1),
  metadataElementId bigint default NULL,
  enumeratedTypeValueId bigint default NULL,
  PRIMARY KEY  (metadataEnumeratedValueId),
  CONSTRAINT FK1_MetadataEnumeratedValues_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataEnumeratedValues_EnumeratedTypeValues')
   DROP INDEX metadataenumeratedvalues.FK1_MetadataEnumeratedValues_EnumeratedTypeValues
;
CREATE INDEX FK1_MetadataEnumeratedValues_EnumeratedTypeValues
	 ON metadataenumeratedvalues(enumeratedTypeValueId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name = 'FK1_MetadataEnumeratedValues_MetadataElements')
   DROP INDEX metadataenumeratedvalues.FK1_MetadataEnumeratedValues_MetadataElements
;
CREATE INDEX FK1_MetadataEnumeratedValues_MetadataElements
	 ON metadataenumeratedvalues(metadataElementId);

CREATE TABLE metadatanumericvalues (
  metadataNumericValueId bigint NOT NULL IDENTITY(1,1),
  metadataElementId bigint default NULL,
  value float(53) default NULL,
  PRIMARY KEY  (metadataNumericValueId),
  CONSTRAINT FK1_MetadataNumericValues_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_MetadataNumericValues_MetadataElements')
  DROP INDEX metadatanumericvalues.FK1_MetadataNumericValues_MetadataElements
;
CREATE INDEX FK1_MetadataNumericValues_MetadataElements
ON metadatanumericvalues(metadataElementId);

CREATE TABLE metadatastringvalues (
  metadataStringValueId bigint NOT NULL IDENTITY(1,1),
  metadataElementId bigint default NULL,
  value nvarchar(max),
  PRIMARY KEY  (metadataStringValueId),
  CONSTRAINT FK1_MetadataStringValues_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE INDEX FK1_MetadataStringValues_MetadataElements
ON metadatastringvalues(metadataElementId);

CREATE TABLE motioneventframedata (
  motionEventFrameDataId bigint NOT NULL IDENTITY(1,1),
  motionEventId bigint NOT NULL default 0,
  frameId bigint NOT NULL default 0,
  PRIMARY KEY  (motionEventFrameDataId),
  CONSTRAINT FK1_MotionEventFrameData_MotionEvents FOREIGN KEY (motionEventId) REFERENCES motionevents (motionEventId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_MotionEventFrameData_MotionEvents')
	DROP INDEX motioneventframedata.FK1_MotionEventFrameData_MotionEvents
;
CREATE INDEX FK1_MotionEventFrameData_MotionEvents
		  ON motioneventframedata(motionEventId);

CREATE TABLE notes (
  noteId bigint NOT NULL IDENTITY(1,1),
  flagId bigint NOT NULL default 0,
  objectId bigint NOT NULL default 0,
  objectType tinyint default 0,
  PRIMARY KEY  (noteId),
  CONSTRAINT FK1_Notes_Flags FOREIGN KEY (flagId) REFERENCES flags (flagId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Notes_Flags')
	DROP INDEX notes.FK1_Notes_Flags
;
CREATE INDEX FK1_Notes_Flags
ON notes(flagId);

CREATE TABLE noteentries (
  noteEntryId bigint NOT NULL IDENTITY(1,1),
  noteId bigint NOT NULL default 0,
  creationDate datetime2 NOT NULL default '0001-01-01 00:00:00',
  entryText nvarchar(max),
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (noteEntryId),
  CONSTRAINT FK1_NoteEntries_Notes FOREIGN KEY (noteId) REFERENCES notes (noteId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_noteentries_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS ( SELECT	name
			FROM	sysindexes
			WHERE	name = 'FK1_NoteEntries_Notes' ) 
	DROP INDEX noteentries.FK1_NoteEntries_Notes;

CREATE INDEX FK1_NoteEntries_Notes
		  ON noteentries(noteId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_noteentries_users')
	DROP INDEX noteentries.FK1_noteentries_users;

CREATE INDEX FK1_noteentries_users
ON noteentries(userguid);

CREATE TABLE panoramaviews (
  panoramaviewId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(100) NOT NULL,
  [view] nvarchar(max) NOT NULL,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (panoramaviewId),
  CONSTRAINT FK1_panoramaviews_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_panoramaviews_users')
		  DROP INDEX panoramaviews.FK1_panoramaviews_users;
		  CREATE INDEX FK1_panoramaviews_users
		  ON panoramaviews(userguid);

CREATE TABLE parameterchangechannels (
  parameterchangedatasourceid bigint NOT NULL IDENTITY(1,1),
  parameterchangeid bigint default NULL,
  channelid bigint default NULL,
  PRIMARY KEY  (parameterchangedatasourceid),
  CONSTRAINT FK1_parameterchangeChannel_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE,
  CONSTRAINT FK1_parameterchangeChannel_parameterchanges FOREIGN KEY (parameterchangeid) REFERENCES parameterchanges (parameterchangeid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_parameterchangeChannel_parameterchanges')
		  DROP INDEX parameterchangechannels.FK1_parameterchangeChannel_parameterchanges;
		  CREATE INDEX FK1_parameterchangeChannel_parameterchanges
		  ON parameterchangechannels(parameterchangeid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_parameterchangeChannel_Channels')
		  DROP INDEX parameterchangechannels.FK1_parameterchangeChannel_Channels;
		  CREATE INDEX FK1_parameterchangeChannel_Channels
		  ON parameterchangechannels(channelid);

CREATE TABLE pendingdeletes (
  directoryName nvarchar(max)
);

CREATE TABLE performance (
  performanceid int NOT NULL IDENTITY(1,1),
  category nvarchar(255) NOT NULL default '',
  [counter] nvarchar(255) NOT NULL default '',
  instance nvarchar(255) default NULL,
  applianceId bigint NOT NULL default 0,
  sampletime datetime2 NOT NULL default '0001-01-01 00:00:00',
  [sample] float NOT NULL default 0,
  PRIMARY KEY  (performanceid),
  CONSTRAINT FK1_Performance_Appliances FOREIGN KEY (applianceId) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='sampletime')
		  DROP INDEX performance.sampletime;
		  CREATE INDEX sampletime
		  ON performance(sampletime);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Performance_Appliances')
		  DROP INDEX performance.FK1_Performance_Appliances;
		  CREATE INDEX FK1_Performance_Appliances
		  ON performance(applianceId);

CREATE TABLE permissions (
  name nvarchar(50) NOT NULL default '',
  roleguid varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  PRIMARY KEY  (roleguid,name),
  CONSTRAINT FK1_permissions_roles FOREIGN KEY (roleguid) REFERENCES roles ([guid])
);

CREATE TABLE personframes (
  personframeid bigint NOT NULL IDENTITY(1,1),
  personid bigint NOT NULL default 0,
  frameid bigint NOT NULL default 0,
  PRIMARY KEY  (personframeid),
  CONSTRAINT FK1_personframes_frames FOREIGN KEY (frameid) REFERENCES frames (frameId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_personframes_persons FOREIGN KEY (personid) REFERENCES persons (personId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_personframes_frames')
		  DROP INDEX personframes.FK1_personframes_frames;
		  CREATE INDEX FK1_personframes_frames
		  ON personframes(frameid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_personframes_persons')
		  DROP INDEX personframes.FK1_personframes_persons;
		  CREATE INDEX FK1_personframes_persons
		  ON personframes(personid);

CREATE TABLE personmetadataelements (
  personMetadataElementId bigint NOT NULL IDENTITY(1,1),
  personId bigint default NULL,
  metadataElementId bigint default NULL,
  PRIMARY KEY  (personMetadataElementId),
  CONSTRAINT FK1_PersonMetadataElements_EventsIndex FOREIGN KEY (personId) REFERENCES persons (personId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_PersonMetadataElements_MetadataElements FOREIGN KEY (metadataElementId) REFERENCES metadataelements (metadataElementId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PersonMetadataElements_MetadataElements')
		  DROP INDEX personmetadataelements.FK1_PersonMetadataElements_MetadataElements;
		  CREATE INDEX FK1_PersonMetadataElements_MetadataElements
		  ON personmetadataelements(metadataElementId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PersonMetadataElements_EventsIndex')
		  DROP INDEX personmetadataelements.FK1_PersonMetadataElements_EventsIndex;
		  CREATE INDEX FK1_PersonMetadataElements_EventsIndex
		  ON personmetadataelements(personId);

CREATE TABLE personprofilegroups (
  personprofilegroupId bigint NOT NULL IDENTITY(1,1),
  personId bigint NOT NULL default 0,
  profileGroupId bigint NOT NULL default 0,
  PRIMARY KEY  (personprofilegroupId),
  CONSTRAINT FK1_PersonProfileGroups_Persons FOREIGN KEY (personId) REFERENCES persons (personId) ON DELETE CASCADE ON UPDATE CASCADE,
 --change to trigger
  CONSTRAINT FK1_PersonProfileGroups_ProfileGroups FOREIGN KEY (profileGroupId) REFERENCES profilegroups (profileGroupId) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PersonProfileGroups_Persons')
		  DROP INDEX personprofilegroups.FK1_PersonProfileGroups_Persons;
		  CREATE INDEX FK1_PersonProfileGroups_Persons
		  ON personprofilegroups(personId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PersonProfileGroups_ProfileGroups')
		  DROP INDEX personprofilegroups.FK1_PersonProfileGroups_ProfileGroups;
		  CREATE INDEX FK1_PersonProfileGroups_ProfileGroups
		  ON personprofilegroups(profileGroupId);

CREATE TABLE plugins (
  pluginId bigint NOT NULL IDENTITY(1,1),
  parentId bigint default NULL,
  [type] tinyint NOT NULL default 0,
  name nvarchar(255) NOT NULL default '',
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  isDisplayed tinyint default 0,
  isEnabled tinyint default 0,
  configXml nvarchar(max),
  md5Checksum nvarchar(100) default NULL,
  updated datetime2 NOT NULL default '0001-01-01 00:00:00',
  [version] nvarchar(20) default NULL,
  --driverType enum('channelinputdriver','videodriver','ptzdriver','streamedvideodriver','channelsettingsdriver') default NULL,
  driverType nvarchar(50) CHECK (driverType IN('channelinputdriver','videodriver','ptzdriver','streamedvideodriver','channelsettingsdriver'))default NULL,
  isUserDefined tinyint default 0,
  dataDirectory nvarchar(100) default NULL,
  relativepath nvarchar(100) default NULL,
  [executable] nvarchar(100) NOT NULL default '',
  PRIMARY KEY  (pluginId),
  CONSTRAINT plugins_guid UNIQUE ([guid]),
  --change to trigger
  CONSTRAINT FK1_Plugins_Plugins FOREIGN KEY (parentId) REFERENCES plugins (pluginId)
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Plugins_Plugins')
		  DROP INDEX plugins.FK1_Plugins_Plugins;
		  CREATE INDEX FK1_Plugins_Plugins
		  ON plugins(parentId);

CREATE TABLE plugingenericeventdefinitions (
  plugingenericeventdefinitionid bigint NOT NULL IDENTITY(1,1),
  pluginId bigint NOT NULL default 0,
  genericEventDefinitionId bigint default NULL,
  PRIMARY KEY  (plugingenericeventdefinitionid),
  CONSTRAINT FK1_pluginGEDs_GEDs FOREIGN KEY (genericEventDefinitionId) REFERENCES genericeventdefinitions (genericEventDefinitionId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_plugingenericeventdefinitions_Plugins FOREIGN KEY (pluginId) REFERENCES plugins (pluginId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_plugingenericeventdefinitions_plugins')
		  DROP INDEX plugingenericeventdefinitions.FK1_plugingenericeventdefinitions_plugins;
		  CREATE INDEX FK1_plugingenericeventdefinitions_plugins
		  ON plugingenericeventdefinitions(pluginId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_pluginGEDs_GEDs')
		  DROP INDEX plugingenericeventdefinitions.FK1_pluginGEDs_GEDs;
		  CREATE INDEX FK1_pluginGEDs_GEDs
		  ON plugingenericeventdefinitions(genericEventDefinitionId);

CREATE TABLE pluginsettingvalues (
  pluginsettingvalueid bigint NOT NULL IDENTITY(1,1),
  settingid nvarchar(255) default NULL,
  value nvarchar(max),
  isUserModified tinyint default 0,
  pluginid bigint NOT NULL,
  channelId bigint default NULL,
  PRIMARY KEY  (pluginsettingvalueid),
  CONSTRAINT FK1_PluginSettingValues_Channels FOREIGN KEY (channelId) REFERENCES channels (channelid) ON DELETE CASCADE,
  CONSTRAINT FK1_PluginSettingValues_Plugins FOREIGN KEY (pluginid) REFERENCES plugins (pluginId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PluginSettingValues_Channels')
		  DROP INDEX pluginsettingvalues.FK1_PluginSettingValues_Channels;
		  CREATE INDEX FK1_PluginSettingValues_Channels
		  ON pluginsettingvalues(channelId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_PluginSettingValues_Plugins')
		  DROP INDEX pluginsettingvalues.FK1_PluginSettingValues_Plugins;
		  CREATE INDEX FK1_PluginSettingValues_Plugins
		  ON pluginsettingvalues(pluginid);

CREATE TABLE profileframes (
  profileFrameId bigint NOT NULL IDENTITY(1,1),
  profileId bigint NOT NULL default 0,
  faceX float(53) NOT NULL default 0,
  faceY float(53) NOT NULL default 0,
  faceWidth float(53) NOT NULL default 0,
  image varbinary(max) NOT NULL,
  PRIMARY KEY  (profileFrameId),
  CONSTRAINT FK1_ProfileFrames_Profiles FOREIGN KEY (profileId) REFERENCES profiles (profileId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_ProfileFrames_Profiles')
		  DROP INDEX profileframes.FK1_ProfileFrames_Profiles;
		  CREATE INDEX FK1_ProfileFrames_Profiles
		  ON profileframes(profileId);

CREATE TABLE profileframesframes (
  profileframesframesid bigint NOT NULL IDENTITY(1,1),
  profileframeid bigint NOT NULL default 0,
  frameid bigint default NULL,
  PRIMARY KEY  (profileframesframesid),
  CONSTRAINT FK1_profileframesframes_frames FOREIGN KEY (frameid) REFERENCES frames (frameId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_profileframesframes_profileframes FOREIGN KEY (profileframeid) REFERENCES profileframes (profileFrameId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_profileframesframes_frames')
		  DROP INDEX profileframesframes.FK1_profileframesframes_frames;
		  CREATE INDEX FK1_profileframesframes_frames
		  ON profileframesframes(frameid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_profileframesframes_profileframes')
		  DROP INDEX profileframesframes.FK1_profileframesframes_profileframes;
		  CREATE INDEX FK1_profileframesframes_profileframes
		  ON profileframesframes(profileframeid);

CREATE TABLE profilegroupprofiles (
  profileGroupProfileId bigint NOT NULL IDENTITY(1,1),
  profileGroupId bigint NOT NULL default 0,
  profileId bigint NOT NULL default 0,
  PRIMARY KEY  (profileGroupProfileId),
  CONSTRAINT FK1_ProfileGroupProfiles_ProfileGroups FOREIGN KEY (profileGroupId) REFERENCES profilegroups (profileGroupId) ON DELETE CASCADE ON UPDATE CASCADE,
 --change to trigger
  CONSTRAINT FK1_ProfileGroupProfiles_Profiles FOREIGN KEY (profileId) REFERENCES profiles (profileId) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_ProfileGroupProfiles_ProfileGroups')
		  DROP INDEX profilegroupprofiles.FK1_ProfileGroupProfiles_ProfileGroups;
		  CREATE INDEX FK1_ProfileGroupProfiles_ProfileGroups
		  ON profilegroupprofiles(profileGroupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_ProfileGroupProfiles_Profiles')
		  DROP INDEX profilegroupprofiles.FK1_ProfileGroupProfiles_Profiles;
		  CREATE INDEX FK1_ProfileGroupProfiles_Profiles
		  ON profilegroupprofiles(profileId);

CREATE TABLE regions (
  regionid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) NOT NULL,
  [root] tinyint NOT NULL default 0,
  PRIMARY KEY  (regionid),
  CONSTRAINT region_name_unique UNIQUE (name)
);

CREATE TABLE regionappliances (
  regionapplianceid bigint NOT NULL IDENTITY(1,1),
  regionid bigint default NULL,
  applianceid bigint default NULL,
  PRIMARY KEY  (regionapplianceid),
  CONSTRAINT FK1_Appliance_Appliance FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE,
  CONSTRAINT FK1_Region_Appliance FOREIGN KEY (regionid) REFERENCES regions (regionid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Appliance_Appliance')
		  DROP INDEX regionappliances.FK1_Appliance_Appliance;
		  CREATE INDEX FK1_Appliance_Appliance
		  ON regionappliances(applianceid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Region_Appliance')
		  DROP INDEX regionappliances.FK1_Region_Appliance;
		  CREATE INDEX FK1_Region_Appliance
		  ON regionappliances(regionid);

CREATE TABLE reportdefinitions (
  reportDefinitionId bigint NOT NULL IDENTITY(1,1),
  reporttype tinyint NOT NULL default 0,
  name nvarchar(255) NOT NULL default '',
  [format] tinyint NOT NULL default 0,
  [description] nvarchar(128) default NULL,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  [xml] nvarchar(max),
  schedulexml nvarchar(max),
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (reportDefinitionId),
  CONSTRAINT FK1_reportdefinitions_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_reportdefinitions_users')
		  DROP INDEX reportdefinitions.FK1_reportdefinitions_users;
		  CREATE INDEX FK1_reportdefinitions_users
		  ON reportdefinitions(userguid);

CREATE TABLE reportappliances (
  reportapplianceid bigint NOT NULL IDENTITY(1,1),
  reportdefinitionid bigint default NULL,
  applianceid bigint default NULL,
  PRIMARY KEY  (reportapplianceid),
  CONSTRAINT FK1_reportappliances_Appliance FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_reportappliances_reportdefinition FOREIGN KEY (reportdefinitionid) REFERENCES reportdefinitions (reportDefinitionId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_reportappliances_Appliance')
		  DROP INDEX reportappliances.FK1_reportappliances_Appliance;
		  CREATE INDEX FK1_reportappliances_Appliance
		  ON reportappliances(applianceid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_reportappliances_reportdefinition')
		  DROP INDEX reportappliances.FK1_reportappliances_reportdefinition;
		  CREATE INDEX FK1_reportappliances_reportdefinition
		  ON reportappliances(reportdefinitionid);

CREATE TABLE reportregions (
  reportregionid bigint NOT NULL IDENTITY(1,1),
  reportdefinitionid bigint default NULL,
  regionid bigint default NULL,
  PRIMARY KEY  (reportregionid),
  CONSTRAINT FK1_reportregions_region FOREIGN KEY (regionid) REFERENCES regions (regionid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_reportregions_reportdefinition FOREIGN KEY (reportdefinitionid) REFERENCES reportdefinitions (reportDefinitionId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_reportregions_region')
		  DROP INDEX reportregions.FK1_reportregions_region;
		  CREATE INDEX FK1_reportregions_region
		  ON reportregions(regionid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_reportregions_reportdefinition')
		  DROP INDEX reportregions.FK1_reportregions_reportdefinition;
		  CREATE INDEX FK1_reportregions_reportdefinition
		  ON reportregions(reportdefinitionid);

CREATE TABLE versions (
  versionId bigint NOT NULL IDENTITY(1,1),
  [version] nvarchar(16) NOT NULL default '',
  revision bigint default 0,
  applied datetime2 NOT NULL default '0001-01-01 00:00:00',
  PRIMARY KEY  (versionId)
);

CREATE TABLE revisions (
  revisionId bigint NOT NULL IDENTITY(1,1),
  versionId bigint NOT NULL default 0,
  revision nvarchar(16) NOT NULL default '',
  applied datetime2 NOT NULL default '0001-01-01 00:00:00',
  PRIMARY KEY  (revisionId),
  CONSTRAINT FK1_Revisions_Versions FOREIGN KEY (versionId) REFERENCES versions (versionId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Revisions_Versions')
		  DROP INDEX revisions.FK1_Revisions_Versions;
		  CREATE INDEX FK1_Revisions_Versions
		  ON revisions(versionId);

CREATE TABLE runtimes (
  runTimesId bigint NOT NULL IDENTITY(1,1),
  start datetime2 default NULL,
  [stop] datetime2 default NULL,
  PRIMARY KEY  (runTimesId)
);

CREATE TABLE tasks (
  taskid bigint NOT NULL IDENTITY(1,1),
  method nvarchar(255) NOT NULL,
  class nvarchar(255) NOT NULL,
  context nvarchar(64) NOT NULL,
  argsbytes varbinary(max) NOT NULL,
  resultscommand nvarchar(255) default NULL,
  resultsparams nvarchar(255) default NULL,
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  scheduled datetime2 NOT NULL default '0001-01-01 00:00:00',
  starttime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endtime datetime2 NOT NULL default '0001-01-01 00:00:00',
  [status] nvarchar(64) default NULL,
  trys bigint default 0,
  [message] nvarchar(max),
  PRIMARY KEY  (taskid)
);

CREATE TABLE savedreports (
  savedReportId bigint NOT NULL IDENTITY(1,1),
  displayName nvarchar(128) default NULL,
  name nvarchar(128) default NULL,
  fileLocation nvarchar(128) default NULL,
  reportdefinitionid bigint default NULL,
  taskid bigint default NULL,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (savedReportId),
  CONSTRAINT FK1_ReportSchedules_Reportdefinition FOREIGN KEY (reportdefinitionid) REFERENCES reportdefinitions (reportDefinitionId) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT FK1_savedreports_task FOREIGN KEY (taskid) REFERENCES tasks (taskid) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT FK1_savedreports_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_savedreports_reportdefinition')
		  DROP INDEX savedreports.FK1_savedreports_reportdefinition;
		  CREATE INDEX FK1_savedreports_reportdefinition
		  ON savedreports(reportdefinitionid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_savedreports_task')
		  DROP INDEX savedreports.FK1_savedreports_task;
		  CREATE INDEX FK1_savedreports_task
		  ON savedreports(taskid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_savedreports_users')
		  DROP INDEX savedreports.FK1_savedreports_users;
		  CREATE INDEX FK1_savedreports_users
		  ON savedreports(userguid);

CREATE TABLE savedviews (
  savedviewId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(100) NOT NULL,
  [view] nvarchar(max) NOT NULL,
  shared tinyint default 0,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (savedviewId),
  --KEY FK1_savedviews_users (userguid),
  CONSTRAINT FK1_savedviews_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_savedviews_users')
		  DROP INDEX savedviews.FK1_savedviews_users;
		  CREATE INDEX FK1_savedviews_users
		  ON savedviews(userguid);

CREATE TABLE settinggroups (
  settingGroupId bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  PRIMARY KEY  (settingGroupId)
);

CREATE TABLE schedules (
  scheduleid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(50) default NULL,
  valuesetid bigint default NULL,
  settingGroupId bigint default NULL,
  PRIMARY KEY  (scheduleid),
  CONSTRAINT FK1_Schedules_SettingGroups FOREIGN KEY (settingGroupId) REFERENCES settinggroups (settingGroupId) ON DELETE CASCADE,
  CONSTRAINT fk1_schedule_valuesets FOREIGN KEY (valuesetid) REFERENCES valuesets (valuesetid) ON DELETE SET NULL
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='fk1_schedule_valuesets')
		  DROP INDEX schedules.fk1_schedule_valuesets;
		  CREATE INDEX fk1_schedule_valuesets
		  ON schedules(valuesetid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Schedules_SettingGroups')
		  DROP INDEX schedules.FK1_Schedules_SettingGroups;
		  CREATE INDEX FK1_Schedules_SettingGroups
		  ON schedules(settingGroupId);

CREATE TABLE scheduletimes (
  scheduletimeid bigint NOT NULL IDENTITY(1,1),
  scheduleid bigint default NULL,
  starttime nvarchar(25) default NULL,
  endtime nvarchar(25) default NULL,
  PRIMARY KEY  (scheduletimeid),
  CONSTRAINT fk1_scheduletime_schedules FOREIGN KEY (scheduleid) REFERENCES schedules (scheduleid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='fk1_scheduletime_schedules')
		  DROP INDEX scheduletimes.fk1_scheduletime_schedules;
		  CREATE INDEX fk1_scheduletime_schedules
		  ON scheduletimes(scheduleid);

CREATE TABLE schemaversions (
  [current] bigint default 0
);

CREATE TABLE seprobanalarms (
  seprobanalarmid bigint NOT NULL IDENTITY(1,1),
  applianceid bigint NOT NULL default 0,
  [state] nvarchar(32) NOT NULL default 'New',
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  timeOffset bigint NOT NULL default 0,
  endTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  [open] tinyint NOT NULL default 1,
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  summary nvarchar(128) NOT NULL default '0/0',
  PRIMARY KEY  (seprobanalarmid),
  CONSTRAINT FK1_seprobanalarms_appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_seprobanalarms_appliances')
		  DROP INDEX seprobanalarms.FK1_seprobanalarms_appliances;
		  CREATE INDEX FK1_seprobanalarms_appliances
		  ON seprobanalarms(applianceid);

CREATE TABLE seprobanimages (
  seprobanimageid bigint NOT NULL IDENTITY(1,1),
  seprobanalarmid bigint NOT NULL default 0,
  [filename] nvarchar(255) NOT NULL default '',
  location nvarchar(255) NOT NULL default '',
  [sent] tinyint NOT NULL default 0,
  imagenumber bigint NOT NULL default 0,
  PRIMARY KEY  (seprobanimageid),
  CONSTRAINT FK1_seprobanimages_seprobanalarms FOREIGN KEY (seprobanalarmid) REFERENCES seprobanalarms (seprobanalarmid) ON DELETE CASCADE ON UPDATE CASCADE
); 

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_seprobanimages_seprobanalarms')
		  DROP INDEX seprobanimages.FK1_seprobanimages_seprobanalarms;
		  CREATE INDEX FK1_seprobanimages_seprobanalarms
		  ON seprobanimages(seprobanalarmid);

CREATE TABLE services (
  serviceId bigint NOT NULL IDENTITY(1,1),
  applianceId bigint NOT NULL default 0,
  serviceType bigint default NULL,
  [status] bigint default NULL,
  targetApplianceId bigint NOT NULL default 0,
  PRIMARY KEY  (serviceId),
  CONSTRAINT FK1_Services_Appliances FOREIGN KEY (applianceId) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE,
 --change to trigger
  CONSTRAINT FK2_Services_Appliances FOREIGN KEY (targetApplianceId) REFERENCES appliances (applianceid) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Services_Appliances')
		  DROP INDEX services.FK1_Services_Appliances;
		  CREATE INDEX FK1_Services_Appliances
		  ON services(applianceId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK2_Services_Appliances')
		  DROP INDEX services.FK2_Services_Appliances;
		  CREATE INDEX FK2_Services_Appliances
		  ON services(targetApplianceId);

CREATE TABLE settinggroupsettings (
  settingGroupSettingId bigint NOT NULL IDENTITY(1,1),
  settingGroupId bigint default NULL,
  settingId bigint default NULL,
  PRIMARY KEY  (settingGroupSettingId),
  CONSTRAINT FK1_SettingGroupSettings_SettingGroups FOREIGN KEY (settingGroupId) REFERENCES settinggroups (settingGroupId) ON DELETE CASCADE,
  CONSTRAINT FK1_SettingGroupSettings_Settings FOREIGN KEY (settingId) REFERENCES settings (settingId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_SettingGroupSettings_SettingGroups')
		  DROP INDEX settinggroupsettings.FK1_SettingGroupSettings_SettingGroups;
		  CREATE INDEX FK1_SettingGroupSettings_SettingGroups
		  ON settinggroupsettings(settingGroupId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_SettingGroupSettings_Settings')
		  DROP INDEX settinggroupsettings.FK1_SettingGroupSettings_Settings;
		  CREATE INDEX FK1_SettingGroupSettings_Settings
		  ON settinggroupsettings(settingId);

CREATE TABLE settingvaluesalternatesettingvalues (
  settingvaluesalternatesettingvalueid bigint NOT NULL IDENTITY(1,1),
  settingvalueid bigint default NULL,
  alternatesettingvalueid bigint default NULL,
  PRIMARY KEY  (settingvaluesalternatesettingvalueid),
  CONSTRAINT fk1_settingvaluesalternatesettingvalue_alternatesettingvalues FOREIGN KEY (alternatesettingvalueid) REFERENCES alternatesettingvalues (alternatesettingvalueid) ON DELETE CASCADE
 --change to trigger
 --CONSTRAINT fk1_settingvaluesalternatesettingvalue_settingvalues FOREIGN KEY (settingvalueid) REFERENCES settingvalues (settingValueId) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='fk1_settingvaluesalternatesettingvalue_settingvalues')
		  DROP INDEX settingvaluesalternatesettingvalues.fk1_settingvaluesalternatesettingvalue_settingvalues;
		  CREATE INDEX fk1_settingvaluesalternatesettingvalue_settingvalues
		  ON settingvaluesalternatesettingvalues(settingvalueid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='fk1_settingvaluesalternatesettingvalue_alternatesettingvalues')
		  DROP INDEX settingvaluesalternatesettingvalues.fk1_settingvaluesalternatesettingvalue_alternatesettingvalues;
		  CREATE INDEX fk1_settingvaluesalternatesettingvalue_alternatesettingvalues
		  ON settingvaluesalternatesettingvalues(alternatesettingvalueid);

CREATE TABLE softwareversions (
  softwareversionId bigint NOT NULL IDENTITY(1,1),
  version nvarchar(16) NOT NULL default '',
  applied datetime2 NOT NULL default '0001-01-01 00:00:00',
  PRIMARY KEY  (softwareversionId)
);

CREATE TABLE subregions (
  subregionid bigint NOT NULL IDENTITY(1,1),
  childid bigint default NULL,
  regionid bigint default NULL,
  PRIMARY KEY  (subregionid),
  CONSTRAINT FK1_Region_Child FOREIGN KEY (childid) REFERENCES regions (regionid) ON DELETE CASCADE,
 --change to trigger
 CONSTRAINT FK1_Region_Region FOREIGN KEY (regionid) REFERENCES regions (regionid) 
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Region_Child')
		  DROP INDEX subregions.FK1_Region_Child;
		  CREATE INDEX FK1_Region_Child
		  ON subregions(childid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Region_Region')
		  DROP INDEX subregions.FK1_Region_Region;
		  CREATE INDEX FK1_Region_Region
		  ON subregions(regionid);

CREATE TABLE subsettings (
  subSettingId bigint NOT NULL IDENTITY(1,1),
  parentSettingId bigint default NULL,
  childSettingId bigint default NULL,
  PRIMARY KEY  (subSettingId),
  CONSTRAINT FK1_Settings_SubSettings FOREIGN KEY (parentSettingId) REFERENCES settings (settingId),
  CONSTRAINT FK2_Settings_SubSettings FOREIGN KEY (childSettingId) REFERENCES settings (settingId)
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Settings_SubSettings')
		  DROP INDEX subsettings.FK1_Settings_SubSettings;
		  CREATE INDEX FK1_Settings_SubSettings
		  ON subsettings(parentSettingId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK2_Settings_SubSettings')
		  DROP INDEX subsettings.FK2_Settings_SubSettings;
		  CREATE INDEX FK2_Settings_SubSettings
		  ON subsettings(childSettingId);

CREATE TABLE tasktargets (
  tasktargetId bigint NOT NULL IDENTITY(1,1),
  taskId bigint NOT NULL default 0,
  applianceId bigint default NULL,
  [end] datetime2 NOT NULL default '0001-01-01 00:00:00',
  [status] bigint NOT NULL default 0,
  PRIMARY KEY  (tasktargetId),
  CONSTRAINT FK1_tasktarget_Appliances FOREIGN KEY (applianceId) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_tasktarget_Tasks FOREIGN KEY (taskId) REFERENCES tasks (taskid) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_tasktarget_Appliances')
		  DROP INDEX tasktargets.FK1_tasktarget_Appliances;
		  CREATE INDEX FK1_tasktarget_Appliances
		  ON tasktargets(applianceId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_tasktarget_Tasks')
		  DROP INDEX tasktargets.FK1_tasktarget_Tasks;
		  CREATE INDEX FK1_tasktarget_Tasks
		  ON tasktargets(taskId);

CREATE TABLE trackings (
  trackingId bigint NOT NULL IDENTITY(1,1),
  channelid bigint NOT NULL default 0,
  applianceid bigint NOT NULL default 0,
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  startTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  startMilliseconds smallint NOT NULL default 0,
  endTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endMilliseconds smallint NOT NULL default 0,
  directionMeanX bigint NOT NULL default 0,
  directionMeanY bigint NOT NULL default 0,
  distance float NOT NULL default 0,
  meanWidth bigint NOT NULL default 0,
  meanHeight bigint NOT NULL default 0,
  meanArea float NOT NULL default 0,
  minWidth bigint NOT NULL default 0,
  minHeight bigint NOT NULL default 0,
  minArea float NOT NULL default 0,
  maxWidth bigint NOT NULL default 0,
  maxHeight bigint NOT NULL default 0,
  maxArea bigint NOT NULL default 0,
  shapeMeanAspectRatio float NOT NULL default 0,
  shapeMinAspectRatio float NOT NULL default 0,
  shapeMaxAspectRatio float NOT NULL default 0,
  velocityMeanX float NOT NULL default 0,
  velocityMeanY float NOT NULL default 0,
  velocityModalX0 float NOT NULL default 0,
  velocityModalY0 float NOT NULL default 0,
  velocityModalX1 float NOT NULL default 0,
  velocityModalY1 float NOT NULL default 0,
  velocityModalX2 float NOT NULL default 0,
  velocityModalY2 float NOT NULL default 0,
  colorH smallint NOT NULL default 0,
  colorS smallint NOT NULL default 0,
  colorV smallint NOT NULL default 0,
  colorH0 smallint NOT NULL default 0,
  colorS0 smallint NOT NULL default 0,
  colorV0 smallint NOT NULL default 0,
  colorH1 smallint NOT NULL default 0,
  colorS1 smallint NOT NULL default 0,
  colorV1 smallint NOT NULL default 0,
  colorH2 smallint NOT NULL default 0,
  colorS2 smallint NOT NULL default 0,
  colorV2 smallint NOT NULL default 0,
  PRIMARY KEY  (trackingId),
  CONSTRAINT FK1_trackingPoints_Appliances FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON UPDATE CASCADE,
  CONSTRAINT FK1_trackings_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_trackings_Appliances')
		  DROP INDEX trackings.FK1_trackings_Appliances;
		  CREATE INDEX FK1_trackings_Appliances
		  ON trackings(applianceid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_trackings_Channels')
		  DROP INDEX trackings.FK1_trackings_Channels;
		  CREATE INDEX FK1_trackings_Channels
		  ON trackings(channelid);

CREATE TABLE trackingpoints (
  trackingPointId bigint NOT NULL IDENTITY(1,1),
  trackingId bigint NOT NULL default 0,
  locationTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  locationMilliseconds smallint NOT NULL default 0,
  locationX bigint NOT NULL default 0,
  locationY bigint NOT NULL default 0,
  width bigint NOT NULL default 0,
  height bigint NOT NULL default 0,
  velocityX float NOT NULL default 0,
  velocityY float NOT NULL default 0,
  pointorder bigint NOT NULL default 0,
  PRIMARY KEY  (trackingPointId),
  CONSTRAINT FK1_trackingPointts_trackings FOREIGN KEY (trackingId) REFERENCES trackings (trackingId) ON DELETE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_trackingPoints_trackings')
		  DROP INDEX trackingpoints.FK1_trackingPoints_trackings;
		  CREATE INDEX FK1_trackingPoints_trackings
		  ON trackingpoints(trackingId);

CREATE TABLE trackingtirs (
  trackingTirId bigint NOT NULL IDENTITY(1,1),
  trackingId bigint NOT NULL default 0,
  tir varbinary(max),
  PRIMARY KEY  (trackingTirId),
  CONSTRAINT FK1_trackingTirs_trackings FOREIGN KEY (trackingId) REFERENCES trackings (trackingId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_trackingTirs_trackings')
		  DROP INDEX trackingtirs.FK1_trackingTirs_trackings;
		  CREATE INDEX FK1_trackingTirs_trackings
		  ON trackingtirs(trackingId);

CREATE TABLE uimessages (
  uiMessageid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) NOT NULL default '',
  label nvarchar(255) NOT NULL default '',
  [text] nvarchar(max) NOT NULL,
  PRIMARY KEY  (uiMessageid)
);

CREATE TABLE updatepackages (
  updatepackageId bigint NOT NULL IDENTITY(1,1),
  autoupgrade tinyint NOT NULL default 0,
  [filename] nvarchar(255) NOT NULL,
  filesize bigint default 0,
  [format] tinyint NOT NULL default 0,
  [version] nvarchar(16) default '0.0',
  [description] nvarchar(max),
  [guid] varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  updated datetime2 NOT NULL default '0001-01-01 00:00:00',
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (updatepackageId),
  CONSTRAINT FK1_updatepackages_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updatepackages_users')
		  DROP INDEX updatepackages.FK1_updatepackages_users;
		  CREATE INDEX FK1_updatepackages_users
		  ON updatepackages(userguid);

CREATE TABLE updateschedules (
  updatescheduleId bigint NOT NULL IDENTITY(1,1),
  updatepackageId bigint NOT NULL,
  updatetype tinyint NOT NULL default 0,
  starttime datetime2 NOT NULL default '0001-01-01 00:00:00',
  name nvarchar(255) NOT NULL ,
  updated datetime2 NOT NULL default '0001-01-01 00:00:00',
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  taskId bigint default NULL,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (updatescheduleId),
  CONSTRAINT FK1_updateschedules_task FOREIGN KEY (taskId) REFERENCES tasks (taskid) ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT FK1_updateschedules_updatepackage FOREIGN KEY (updatepackageId) REFERENCES updatepackages (updatepackageId) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_updateschedules_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateschedules_updatepackage')
		  DROP INDEX updateschedules.FK1_updateschedules_updatepackage;
		  CREATE INDEX FK1_updateschedules_updatepackage
		  ON updateschedules(updatepackageId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateschedules_task')
		  DROP INDEX updateschedules.FK1_updateschedules_task;
		  CREATE INDEX FK1_updateschedules_task
		  ON updateschedules(taskId);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateschedules_users')
		  DROP INDEX updateschedules.FK1_updateschedules_users;
		  CREATE INDEX FK1_updateschedules_users
		  ON updateschedules(userguid);

CREATE TABLE updateappliances (
  updateapplianceid bigint NOT NULL IDENTITY(1,1),
  updatescheduleId bigint NOT NULL,
  applianceid bigint default NULL,
  PRIMARY KEY  (updateapplianceid),
  CONSTRAINT FK1_updateappliances_Appliance FOREIGN KEY (applianceid) REFERENCES appliances (applianceid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_updateappliances_updateschedule FOREIGN KEY (updatescheduleId) REFERENCES updateschedules (updatescheduleId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateappliances_Appliance')
		  DROP INDEX updateappliances.FK1_updateappliances_Appliance;
		  CREATE INDEX FK1_updateappliances_Appliance
		  ON updateappliances(applianceid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateappliances_updateschedule')
		  DROP INDEX updateappliances.FK1_updateappliances_updateschedule;
		  CREATE INDEX FK1_updateappliances_updateschedule
		  ON updateappliances(updatescheduleId);

CREATE TABLE updateregions (
  updateregionid bigint NOT NULL IDENTITY(1,1),
  updatescheduleId bigint NOT NULL,
  regionid bigint NOT NULL,
  PRIMARY KEY  (updateregionid),
  CONSTRAINT FK1_updateregions_region FOREIGN KEY (regionid) REFERENCES regions (regionid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_updateregions_updateschedule FOREIGN KEY (updatescheduleId) REFERENCES updateschedules (updatescheduleId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateregions_region')
		  DROP INDEX updateregions.FK1_updateregions_region;
		  CREATE INDEX FK1_updateregions_region
		  ON updateregions(regionid);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_updateregions_updateschedule')
		  DROP INDEX updateregions.FK1_updateregions_updateschedule;
		  CREATE INDEX FK1_updateregions_updateschedule
		  ON updateregions(updatescheduleId);

CREATE TABLE userappliances (
  userguid varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  applianceguid varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  PRIMARY KEY  (userguid,applianceguid),
  CONSTRAINT FK1_userappliances_appliances FOREIGN KEY (applianceguid) REFERENCES appliances ([guid]),
  CONSTRAINT FK1_userappliances_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_userappliances_appliances')
		  DROP INDEX userappliances.FK1_userappliances_appliances;
		  CREATE INDEX FK1_userappliances_appliances
		  ON userappliances(applianceguid);

CREATE TABLE useroldpasswords (
  useroldpasswordid bigint NOT NULL IDENTITY(1,1),
  [password] nvarchar(50) NOT NULL default '',
  created datetime2 NOT NULL default '0001-01-01 00:00:00',
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (useroldpasswordid),
  CONSTRAINT FK1_useroldpasswords_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_useroldpasswords_users')
		  DROP INDEX useroldpasswords.FK1_useroldpasswords_users;
		  CREATE INDEX FK1_useroldpasswords_users
		  ON useroldpasswords(userguid);

CREATE TABLE userpreferences (
  userpreferenceid bigint NOT NULL IDENTITY(1,1),
  name nvarchar(255) NOT NULL,
  value nvarchar(max) NOT NULL,
  userguid varbinary(16) default NULL,
  PRIMARY KEY  (userpreferenceid),
  CONSTRAINT FK1_userpreferences_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_userpreferences_users')
		  DROP INDEX userpreferences.FK1_userpreferences_users;
		  CREATE INDEX FK1_userpreferences_users
		  ON userpreferences(userguid);

CREATE TABLE userregions (
  userguid varbinary(16) NOT NULL default 0x00000000000000000000000000000000,
  regionname nvarchar(255) NOT NULL default '',
  PRIMARY KEY  (userguid,regionname),
  CONSTRAINT FK1_userregions_regions FOREIGN KEY (regionname) REFERENCES regions (name) ,
  CONSTRAINT FK1_userregions_users FOREIGN KEY (userguid) REFERENCES users ([guid])
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_userregions_regions')
		  DROP INDEX userregions.FK1_userregions_regions;
		  CREATE INDEX FK1_userregions_regions
		  ON userregions(regionname);

CREATE TABLE videos (
  videoId bigint NOT NULL IDENTITY(1,1),
  channelid bigint NOT NULL default 0,
  deviceId bigint NOT NULL default 0,
  beginTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  beginMilliseconds smallint NOT NULL default 0,
  endTime datetime2 NOT NULL default '0001-01-01 00:00:00',
  endMilliseconds smallint NOT NULL default 0,
  [filename] nvarchar(128) NOT NULL default '',
  width smallint NOT NULL default 704,
  height smallint NOT NULL default 480,
  pixelAspectRatioNominal tinyint NOT NULL default 1,
  pixelAspectRatioAdjustment float NOT NULL default 1.1,
  [format] tinyint NOT NULL default 1,
  fileSize bigint NOT NULL default 0,
  saved tinyint default 0,
  PRIMARY KEY  (videoId),
  CONSTRAINT FK1_Videos_Channels FOREIGN KEY (channelid) REFERENCES channels (channelid) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT FK1_Videos_Devices FOREIGN KEY (deviceId) REFERENCES devices (deviceId) ON DELETE CASCADE ON UPDATE CASCADE
);

IF EXISTS (SELECT name FROM sysindexes
		  WHERE name='FK1_Videos_Devices')
		  DROP INDEX videos.FK1_Videos_Devices;
		  CREATE INDEX FK1_Videos_Devices
		  ON videos(deviceId);

		  CREATE INDEX beginTime
		  ON videos(beginTime);

		  CREATE INDEX endTime
		  ON videos(endTime);

		  CREATE INDEX datasource_begintime
		  ON videos(channelid,beginTime);

CREATE TABLE videoviews (
  videoViewId   int IDENTITY(1,1) NOT NULL,
  name          nvarchar(255) NOT NULL DEFAULT '',
  channelviews  nvarchar(max) NOT NULL,
  PRIMARY KEY (videoViewId)
);

-- Merge from Master branch begin
CREATE TABLE eventframes
	(
	  eventframeid BIGINT NOT NULL
						  IDENTITY(1, 1) ,
	  eventid BIGINT NOT NULL ,
	  frameid BIGINT NOT NULL ,
	  PRIMARY KEY ( eventframeid )
	);

ALTER TABLE eventframes
ADD 
CONSTRAINT FK1_EventFrames_Frames FOREIGN KEY (frameid) REFERENCES frames (frameid) ON DELETE CASCADE ON UPDATE CASCADE,
CONSTRAINT FK1_EventFrames_EventsIndex FOREIGN KEY (eventid) REFERENCES eventsindex (eventid) ON DELETE CASCADE ON UPDATE CASCADE
;
CREATE TABLE motioneventuserframes(
motioneventuserframeid bigint not null IDENTITY(1,1), 
motioneventid bigint not null, 
framemotiondataid bigint not null,
primary key (motioneventuserframeid)
);

ALTER TABLE motioneventuserframes
ADD
CONSTRAINT FK1_MotionEventUserFrames_FrameMotionData FOREIGN KEY (framemotiondataid) REFERENCES framemotiondata (framemotiondataid) ON DELETE CASCADE ON UPDATE CASCADE,
CONSTRAINT FK1_MotionEventUserFrames_MotionEvents FOREIGN KEY (motioneventid) REFERENCES motionevents (motioneventid)
;
INSERT INTO motioneventuserframes(motioneventid, framemotiondataid) select motioneventid, framemotiondataid from framemotiondata where usercreated=1;
-- Merge from Master branch end
--All triggers.Because MSSQL can't support the cascade delete in circule tables.So we use trigger instead of cascade delete
GO

CREATE TRIGGER channels_trigD
ON channels
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS (SELECT *
				 FROM   channelcontrolpatterns ccp,
						deleted dccp
				 WHERE  ccp.channelid = dccp.channelid)
		BEGIN
			DELETE FROM channelcontrolpatterns
			WHERE  channelid IN (SELECT channelid
								 FROM   deleted)
		END
	   IF EXISTS(SELECT *
				FROM   alarmportchannels,
					   deleted
				WHERE  alarmportchannels.channelid = deleted.channelid)
		BEGIN
			DELETE FROM alarmportchannels
			WHERE  channelid IN(SELECT channelid
								FROM   deleted)
		END

	  IF EXISTS(SELECT *
				FROM   audiorecording,
					   deleted
				WHERE  audiorecording.channelid = deleted.channelid)
		BEGIN
			DELETE FROM audiorecording
			WHERE  channelid IN(SELECT channelid
								FROM   deleted)
		END

	  IF EXISTS(SELECT *
				FROM   persons,
					   deleted
				WHERE  persons.firstSeenChannelid = deleted.channelid)
		BEGIN
			DELETE FROM persons
			WHERE  firstSeenChannelid IN(SELECT channelid
										 FROM   deleted)
		END

	  IF EXISTS(SELECT *
				FROM   profilegroups,
					   deleted
				WHERE  profilegroups.creationChannelid = deleted.channelid)
		BEGIN
			DELETE FROM profilegroups
			WHERE  creationChannelid IN(SELECT channelid
										FROM   deleted)
		END

	  IF EXISTS(SELECT *
				FROM   eventsindex,
					   deleted
				WHERE  eventsindex.channelid = deleted.channelid)
		BEGIN
			DELETE FROM eventsindex
			WHERE  channelid IN(SELECT channelid
								FROM   deleted)
		END

	  DELETE FROM channels
	  WHERE  channelid IN (SELECT channelid
						   FROM   deleted)
  END

GO
--1. Remove the 2nd foreign key, and implement AFTER INSERT and UPDATE trigger on table settingvaluesalternatesettingvalues:
-- AFTER INSERT/UPDATE trigger: make sure inserted/updated new settingvalueid exists in parent table settingvalues.
--2. Add AFTER DELETE trigger on table settingvalues to make sure corresponding records in child table settingvaluesalternatesettingvalues is deleted.

CREATE TRIGGER settingvaluesalternatesettingvalues_trigI
ON settingvaluesalternatesettingvalues
AFTER INSERT, UPDATE
AS
  BEGIN
	  IF EXISTS(SELECT settingvalueid
				FROM   inserted
				WHERE  settingvalueid NOT IN (SELECT settingvalueid
											  FROM   settingvalues))
		BEGIN
			RAISERROR('foreign key between settingvaluesalternatesettingvalues.settingvalueid and settingvalues.settingvalueid violation',10,1)

			ROLLBACK TRANSACTION

			RETURN
		END
  END

GO

CREATE TRIGGER settingvalues_trigD
ON settingvalues
AFTER DELETE
AS
  BEGIN
	  IF EXISTS(SELECT settingvalueid
				FROM   settingvaluesalternatesettingvalues
				WHERE  settingvalueid IN (SELECT settingvalueid
										  FROM   deleted))
		BEGIN
			DELETE FROM settingvaluesalternatesettingvalues
			WHERE  settingvalueid IN (SELECT settingvalueid
									  FROM   deleted)
		END
  END

GO

CREATE TRIGGER regions_trigD
ON regions
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS (SELECT *
				 FROM   subregions ccp,
						deleted dccp
				 WHERE  ccp.regionid = dccp.regionid)
		BEGIN
			DELETE FROM subregions
			WHERE  regionid IN (SELECT regionid
								FROM   deleted)
		END

	  DELETE FROM regions
	  WHERE  regionid IN (SELECT regionid
						  FROM   deleted)
  END

GO

CREATE TRIGGER profilegroups_trigD
ON profilegroups
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS (SELECT *
				 FROM   persons,
						deleted
				 WHERE  persons.canonicalProfileGroupId = deleted.profileGroupId)
		BEGIN
			DELETE FROM persons
			WHERE  canonicalProfileGroupId IN (SELECT profileGroupId
											   FROM   deleted)
		END

	  IF EXISTS (SELECT *
				 FROM   faceevents,
						deleted
				 WHERE  faceevents.profileGroupId = deleted.profileGroupId)
		BEGIN
			DELETE FROM faceevents
			WHERE  profileGroupId IN (SELECT profileGroupId
									  FROM   deleted)
		END

	  IF EXISTS (SELECT *
				 FROM   personprofilegroups,
						deleted
				 WHERE  personprofilegroups.profileGroupId = deleted.profileGroupId)
		BEGIN
			DELETE FROM personprofilegroups
			WHERE  profileGroupId IN (SELECT profileGroupId
									  FROM   deleted)
		END

	  DELETE FROM profilegroups
	  WHERE  profileGroupId IN (SELECT profileGroupId
								FROM   deleted)
  END

GO

CREATE TRIGGER profiles_trigD
ON profiles
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS (SELECT *
				 FROM   profilegroups ccp,
						deleted dccp
				 WHERE  ccp.canonicalProfileId = dccp.profileId)
		BEGIN
			DELETE FROM profilegroups
			WHERE  canonicalProfileId IN (SELECT profileId
										  FROM   deleted)
		END

	  IF EXISTS(SELECT *
				FROM   profilegroupprofiles,
					   DELETED
				WHERE  profilegroupprofiles.profileId = DELETED.profileId)
		BEGIN
			DELETE FROM profilegroupprofiles
			WHERE  profileId IN (SELECT profileId
								 FROM   deleted)
		END

	  DELETE FROM profiles
	  WHERE  profileId IN (SELECT profileId
						   FROM   deleted)
  END

GO

CREATE TRIGGER eventsindex_trigD
ON eventsindex
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS (SELECT *
				 FROM   importedimageevents ccp,
						deleted dccp
				 WHERE  ccp.eventId = dccp.eventId)
		BEGIN
			DELETE FROM importedimageevents
			WHERE  eventId IN (SELECT eventId
							   FROM   deleted)
		END

	  IF EXISTS (SELECT *
				 FROM   motionevents ccp,
						deleted dccp
				 WHERE  ccp.eventId = dccp.eventId)
		BEGIN
			DELETE FROM motionevents
			WHERE  eventId IN (SELECT eventId
							   FROM   deleted)
		END

	  DELETE FROM eventsindex
	  WHERE  eventId IN (SELECT eventId
						 FROM   deleted)
  END

GO

CREATE TRIGGER motionevents_trigD
ON motionevents
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS(SELECT *
				FROM   motioneventuserframes,
					   DELETED
				WHERE  motioneventuserframes.motioneventid = DELETED.motioneventid)
		BEGIN
			DELETE FROM motioneventuserframes
			WHERE  motioneventid IN (SELECT motioneventid
									 FROM   deleted)
		END

	  DELETE FROM motionevents
	  WHERE  motioneventid IN (SELECT motioneventid
							   FROM   deleted)
  END

GO
--For this trigger,now when we delete one appliance using sql stmt,it will be wrong,we have the same result in MYSQL.
--in this script,there are so many other tables reference appliance table,but there's no delete action like CASCADE DELETE.
--because now we are not very clear why we design these tables like this,so in MSSQL we just did it like MYSQL.
CREATE TRIGGER appliances_trigD
ON appliances
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS(SELECT *
				FROM   services,
					   DELETED
				WHERE  services.targetApplianceId = DELETED.applianceid)
		BEGIN
			DELETE FROM services
			WHERE  targetApplianceId IN (SELECT applianceid
										 FROM   deleted)
		END

	  DELETE FROM appliances
	  WHERE  applianceid IN (SELECT applianceid
							 FROM   deleted)
  END

GO
--in plugins table,it has a self-reference,and if we use cascade delete in MSSQL,it will have an error,so we use trigger instead of it
CREATE TRIGGER plugins_trigD
ON plugins
INSTEAD OF DELETE
AS
  BEGIN
	  SET NOCOUNT ON

	  IF EXISTS(SELECT *
				FROM   plugins,
					   DELETED
				WHERE  plugins.parentId = DELETED.pluginId)
		BEGIN
			DELETE FROM plugins
			WHERE  parentId IN (SELECT pluginId
								FROM   deleted)
		END

	  DELETE FROM plugins
	  WHERE  pluginId IN (SELECT pluginId
						  FROM   deleted)
  END

GO

--SET FOREIGN_KEY_CHECKS=1; 
--INSERT INTO casestatuses VALUES (1,'Open'),(2,'Closed');
INSERT INTO casestatuses VALUES ('Open'),('Closed');
--INSERT INTO metadatakeys VALUES 
--(1,'Employee ID',NULL,1,1,0,0x9F53EC0C9EB51E4F8175D7816270BBB1,0,0,'',0,0,0,0,0,0,0,0,0),
--(2,'Gender',NULL,4,1,0,0x071611A4DF2C9B44BCB08425ECED1286,0,0,'',0,0,0,0,0,0,0,0,0),
--(3,'Hair Color',NULL,4,1,0,0x226A7707901B0D41814A27091E7B0122,0,0,'',0,0,0,0,0,0,0,0,0);
INSERT INTO metadatakeys VALUES 
('Employee ID',NULL,1,1,0,0x9F53EC0C9EB51E4F8175D7816270BBB1,0,0,'',0,0,0,0,0,0,0,0,0),
('Gender',NULL,4,1,0,0x071611A4DF2C9B44BCB08425ECED1286,0,0,'',0,0,0,0,0,0,0,0,0),
('Hair Color',NULL,4,1,0,0x226A7707901B0D41814A27091E7B0122,0,0,'',0,0,0,0,0,0,0,0,0);
--INSERT INTO enumeratedtypevalues VALUES (1,2,'Male'),(2,2,'Female'),(3,3,'Bald'),(4,3,'Black'),(5,3,'Blonde'),(6,3,'Brown'),(7,3,'Gray'),(8,3,'Red');
INSERT INTO enumeratedtypevalues VALUES (2,'Male'),(2,'Female'),(3,'Bald'),(3,'Black'),(3,'Blonde'),(3,'Brown'),(3,'Gray'),(3,'Red');
INSERT INTO eventcleanupinfo VALUES (1, getutcdate(), 0);
INSERT INTO eventtypetrimmingschedules (eventtypeid, daysbeforedataexpiration) values (1, 90), (2,90);
--INSERT INTO flags VALUES (1,'Note',1,0),(2,'Flag',1,0),(3,'Review',1,0),(4,'Cleared',1,0);
INSERT INTO flags VALUES ('Note',1,0),('Flag',1,0),('Review',1,0),('Cleared',1,0);
--INSERT INTO groups VALUES (1,'All People','All People',1,1,0),(2,'Contractors','Contractors',0,0,0),(3,'Employees','Employees',0,0,0),(4,'Maintenance','Maintenance',0,0,0),(5,'Suspicious People','Suspicious People',0,0,0),(6,'Uncategorized People','Uncategorized People',1,3,0),(7,'Unknown People','Unknown People',0,0,0),(8,'Vendors','Vendors',0,0,0),(9,'Visitors','Visitors',0,0,0),(10,'Categorized People','Categorized People',1,2,0),(11,'Imported People','Imported People',1,4,0);
INSERT	INTO groups
VALUES	( 'All People', 'All People', 1, 1, 0 ),
		( 'Contractors', 'Contractors', 0, 0, 0 ),
		( 'Employees', 'Employees', 0, 0, 0 ),
		( 'Maintenance', 'Maintenance', 0, 0, 0 ),
		( 'Suspicious People', 'Suspicious People', 0, 0, 0 ),
		( 'Uncategorized People', 'Uncategorized People', 1, 3, 0 ),
		( 'Unknown People', 'Unknown People', 0, 0, 0 ),
		( 'Vendors', 'Vendors', 0, 0, 0 ),
		( 'Visitors', 'Visitors', 0, 0, 0 ),
		( 'Categorized People', 'Categorized People', 1, 2, 0 ),
		( 'Imported People', 'Imported People', 1, 4, 0 );
INSERT	INTO roles
VALUES	( 'Nothing', 0x179E790F25FB9C47831C5D8F7D248E62, 1, 1, 7 ),
		( 'Integrator', 0xAEA78ED34B93504CA260ED181A4C42EA, 1, 0, 7 ),
		( 'User', 0xB459450D7009CB49B397244EC08B33F2, 1, 0, 7 ),
		( 'Engineer', 0xD7BD04FB7D70874C98ED63749E2215AA, 1, 0, 7 ),
		( 'Administrator', 0x7F031F2B9C98824CAAB6F1109DBE5375, 1, 0, 7 );
INSERT	INTO permissions
		( roleguid, name )
VALUES	( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessAdministratorFeatures' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessAlertsTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessCasesTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessHealthMonitorAlertsTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessHealthMonitorSummaryTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessIntegratorFeatures' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessManageServersTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessManageUsersRolesTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessManageUsersUsersTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessMonitorTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessPeopleTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessReportsTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessSearchTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessUpdateSoftwareTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'AccessVideoTab' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditAlert' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditAlertAction' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditCase' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditChannelGroups' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditEnterpriseServer' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditEvent' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditPerson' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditPersonGroup' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'EditUsers' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ExportCase' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ExportEvents' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ExportPeople' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ImportCase' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ImportEvents' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'ImportPeople' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'MatchPeople' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'SaveFrame' ),
		( 0xAEA78ED34B93504CA260ED181A4C42EA, 'SelectMultipleChannels' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessAlertsTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessManageUsersUsersTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessMonitorTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessPeopleTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessSearchTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'AccessVideoTab' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'EditEvent' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'EditPerson' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'MatchPeople' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'SaveFrame' ),
		( 0xB459450D7009CB49B397244EC08B33F2, 'SelectMultipleChannels' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessAdministratorFeatures' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessAlertsTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessCasesTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessEngineerFeatures' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessHealthMonitorAlertsTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessHealthMonitorSummaryTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessIntegratorFeatures' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessManageServersTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessManageUsersRolesTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessManageUsersUsersTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessMonitorTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessPeopleTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessReportsTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessSearchTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessUpdateSoftwareTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'AccessVideoTab' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditAlert' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditAlertAction' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditCase' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditChannelGroups' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditEnterpriseServer' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditEvent' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditPerson' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditPersonGroup' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'EditUsers' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ExportCase' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ExportEvents' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ExportPeople' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ImportCase' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ImportEvents' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'ImportPeople' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'MatchPeople' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'SaveFrame' ),
		( 0xD7BD04FB7D70874C98ED63749E2215AA, 'SelectMultipleChannels' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessAdministratorFeatures' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessAlertsTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessCasesTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessHealthMonitorAlertsTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessHealthMonitorSummaryTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessManageServersTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessManageUsersRolesTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessManageUsersUsersTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessMonitorTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessPeopleTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessReportsTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessSearchTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessUpdateSoftwareTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'AccessVideoTab' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditAlert' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditAlertAction' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditCase' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditChannelGroups' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditEvent' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditPerson' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditPersonGroup' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'EditUsers' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ExportCase' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ExportEvents' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ExportPeople' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ImportCase' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ImportEvents' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'ImportPeople' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'MatchPeople' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'SaveFrame' ),
		( 0x7F031F2B9C98824CAAB6F1109DBE5375, 'SelectMultipleChannels' );
INSERT INTO permissions (roleguid, name) select guid, 'AllowAudio' from roles where (name != 'Nothing');  
INSERT INTO permissions (roleguid, name) select guid, 'EditView' from roles where (name != 'Nothing' AND name != 'User');  
INSERT INTO permissions (roleguid, name) select guid, 'ExportVideo' from roles where (name != 'Nothing' AND name != 'User');  
INSERT INTO regions (name,root) SELECT 'Enterprise', '1' FROM (SELECT 'true' AS t) AS testtable WHERE testtable.t='%SETTINGS.ENTERPRISE%';
INSERT INTO schemaversions VALUES (2);
  INSERT	INTO users
			( userid, userName, passwd, firstName, lastname, deleted,
			  [timeout], expires, [expiredate], [guid], passwdChanged,
			  passwordviolations, lastsignindate, roleguid, [sid] )
  VALUES	( 1, '3vr', '', '3vr', 'Engineer', 0, 240, 0,
			  '0001-01-01 00:00:00', 0x49878ABDF2E2CC4793C148BE229C270E,
			  '0001-01-01 00:00:00', 0, '2012-10-26 20:05:36',
			  0xD7BD04FB7D70874C98ED63749E2215AA, NULL ),
			( 2, 'techrep', 'ef/vxlf6nySb3VdYWIW+rSd3QBE=', '3vr',
			  'Technician', 0, 240, 0, '0001-01-01 00:00:00',
			  0x3BDA85D4EBCB8946AE75898889DC6A5C, '0001-01-01 00:00:00', 0,
			  GETUTCDATE(),0xAEA78ED34B93504CA260ED181A4C42EA, NULL ),
			( 3, 'admin', 'fIdUH9Pz71AW4S1BGQDIemBGqOg=', '3vr', 'Admin', 0,
			  240, 0, '0001-01-01 00:00:00',
			  0x975806B2BD019E439E400980A3C337BF, '0001-01-01 00:00:00', 0,
			  GETUTCDATE(),0x7F031F2B9C98824CAAB6F1109DBE5375, NULL ),
			( 4, 'alertviewer', 'FbOnvLGbetNGr0I1g6SAIx72E4M=', '3VR',
			  'AlertViewer', 0, 0, 0, '0001-01-01 00:00:00',
			  0x2FCC346DD474C84A81D03B63686E411B, '0001-01-01 00:00:00', 0,
			  GETUTCDATE(), 0x179E790F25FB9C47831C5D8F7D248E62, NULL ),
			( 5, 'spotmonitor', 'nlNxpDM+wM2BfqEcfXVK7rAvq7A=', '3VR',
			  'SpotMonitor', 0, 0, 0, '0001-01-01 00:00:00',
			  0x64ABB5F92924F44F90E678D7FC8921D2, '0001-01-01 00:00:00', 0,
			  GETUTCDATE(), 0x179E790F25FB9C47831C5D8F7D248E62, NULL );
INSERT INTO userregions (userguid, regionname) SELECT u.guid, r.name FROM users u, regions r WHERE r.name='Enterprise';
--insert into audiorecording (channelid, audiochannelid, deviceid, starttime, endtime) select svS.ComponentId, IF(svS.Value = -1, svS.componentid, svS.Value) , d.deviceid, ADDDATE(UTC_TIMESTAMP(), INTERVAL -1 MONTH), '9000-04-20 06:00:00' from components cS, settingvalues svS, settings sS, settingdefinitions sdS, devices d where d.STATUS = 'Current' and sS.name = 'Audio.Source' and svS.settingdefinitionid = sdS.settingdefinitionid and sS.settingId = sdS.settingId and cS.componentid = svS.componentId and svS.Value != 0;
insert into audiorecording (channelid, audiochannelid, deviceid, starttime, endtime) 
select svS.ComponentId, IIF(svS.Value = -1, svS.componentid, svS.Value) , d.deviceid, 
DATEADD(MONTH,-1,GETUTCDATE()), '9000-04-20 06:00:00' 
from components cS, settingvalues svS, settings sS, settingdefinitions sdS, devices d 
where d.STATUS = 'Current' and sS.name = 'Audio.Source' 
and svS.settingdefinitionid = sdS.settingdefinitionid 
and sS.settingId = sdS.settingId 
and cS.componentid = svS.componentId 
and svS.Value != 0;
--INSERT INTO categories VALUES (1,'None',1,1);
INSERT INTO categories VALUES ('None',1,1);
