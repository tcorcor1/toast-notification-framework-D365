const ENUMS = {
  SDK_TYPE_CODE: {
    CREATE: 214220000,
    UPDATE: 214220001,
  },
  FORM_TYPE: {
    UNDEFINED: 0,
    CREATE: 1,
    UPDATE: 2,
    READ_ONLY: 3,
    DISABLED: 4,
    BULK_EDIT: 6,
  },
};

function setSdkTriggerFieldsRequired(executionContext) {
  const formContext = executionContext.getFormContext();

  const sdkTypeCodeAttribute = formContext.getAttribute("yyz_sdksteptypecode");
  const sdkStepTriggerFieldsAttribute = formContext.getAttribute(
    "yyz_sdksteptriggerfields"
  );

  sdkStepTriggerFieldsAttribute.setRequiredLevel(
    sdkTypeCodeAttribute.getValue() === ENUMS.SDK_TYPE_CODE.UPDATE
      ? "required"
      : "none"
  );
}

function disableSdkStepType(executionContext) {
  const formContext = executionContext.getFormContext();

  if (formContext.ui.getFormType() !== ENUMS.FORM_TYPE.CREATE) {
    formContext.getControl("yyz_sdksteptypecode").setDisabled(true);
  }
}
