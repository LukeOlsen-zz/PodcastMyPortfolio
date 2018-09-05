import React, { Component } from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import 'react-datepicker/dist/react-datepicker.css';
import { isNullOrUndefined, error } from 'util';
import BootstrapTable from 'react-bootstrap-table-next';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';


const validationSchema = function (values) {
  return Yup.object().shape({
  });
};

const validate = (getValidationSchema) => {
  return (values) => {
    const validationSchema = getValidationSchema(values);
    try {
      validationSchema.validateSync(values, { abortEarly: false });
      return {};
    } catch (error) {
      return getErrorsFromValidationError(error);
    }
  };
};

const getErrorsFromValidationError = (validationError) => {
  const FIRST_ERROR = 0;
  return validationError.inner.reduce((errors, error) => {
    return {
      ...errors,
      [error.path]: error.errors[FIRST_ERROR]
    };
  }, {});
};

const onSubmit = (values, { setSubmitting, setErrors, setFieldError }) => {
  if (!isNullOrUndefined(values.datafile) && (values.datafile.type === 'text/csv' || values.datafile.type === 'application/vnd.ms-excel') || values.datafile.type === 'text/plain') {
    const fd = new FormData();
    fd.append("file", values.datafile, 'data.csv');
    axios.post('/api/clients/import', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        values.importResults = response.data;

        // A successful import has one record which tells us how many records got imported. More than 1 indicates errors
        if (values.importResults.length === 1) {
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Clients imported';
          values.updateNoticeStyle = 'success';
        }
        else {
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Errors while importing';
          values.updateNoticeStyle = 'danger';
        }

        setSubmitting(false);
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        if (error.response.status === 500) {
          // Display error for user
          if (!isNullOrUndefined(error.response.data.field)) {
            setFieldError(error.response.data.field, error.response.data.message);
          }
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Problem processing client import';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });
  }
  else {
    setFieldError('datafile', 'Invalid data file. Got "' + values.datafile.type + '" but expected text.');
    setSubmitting(false);
  }
};

class ClientsImport extends Component {
  constructor(props) {
    super(props);
    this.state = {
      datafile: null,
      importResults: [],
      importResultsColumns: [
        {
          dataField: 'result',
          text: 'Import Results'
        }
      ]
    };

    this.touchAll = this.touchAll.bind(this);
  }

  findFirstError(formName, hasError) {
    const form = document.forms[formName];
    for (let i = 0; i < form.length; i++) {
      if (hasError(form[i].name)) {
        form[i].focus();
        break;
      }
    }
  }

  validateForm(errors) {
    this.findFirstError('simpleForm', (fieldName) => {
      return Boolean(errors[fieldName]);
    });
  }

  setUpdateResponse(response) {
    this.setState({ updateNoticeVisible: true });
  }

  touchAll(setTouched, errors) {
    setTouched({
      audiofile: true
    }
    );
    this.validateForm(errors);
  }

  render() {
    return (
      <div className="animated fadein">
        <Formik
            enableReinitialize
            initialValues={{
              datafile: null, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success', importResults: this.state.importResults
            }}
            validate={validate(validationSchema)}
            onSubmit={onSubmit}
            render={
            ({
              values,
              errors,
              touched,
              status,
              dirty,
              handleChange,
              handleBlur,
              handleSubmit,
              isSubmitting,
              isValid,
              handleReset,
              setTouched,
              setFieldValue
              }) => (
                <Form onSubmit={handleSubmit} noValidate name='simpleForm'>
                  <Card>
                    <CardHeader><i className="fa fa-user" /><strong>Client Import</strong>
                    </CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="datafile">Data File</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="file"
                              className="is-valid"
                              name="datafile"
                              id="datafile"
                              placeholder="Client Data File"
                              autoComplete="datafile"
                              valid={!errors.datafile}
                              invalid={touched.datafile && !!errors.datafile}
                              onChange={(event) => { setFieldValue("datafile", event.currentTarget.files[0]); values.updateNoticeVisible = false; values.importResults = []; }}
                          />
                          <FormFeedback>{errors.datafile}</FormFeedback>
                          <FormText>The import file must be in a TAB delimited format with these four fields [Your client group id] [Your client id] [Client name] [Client email]</FormText>
                        </Col>
                      </FormGroup>
                    </CardBody>
                    <CardFooter>
                      <FormGroup row>
                        <Col md="2">
                          <Button type="submit" color="primary" className="mr-1" disabled={isSubmitting || !isValid}>{isSubmitting ? 'Wait...' : 'Submit'}</Button>
                        </Col>
                        <Col md="10">
                          <Alert id="updateResponse" color={values.updateNoticeStyle} isOpen={values.updateNoticeVisible} >{values.updateNoticeMessage}</Alert>
                          <BootstrapTable striped hover keyField='linenumber' data={values.importResults} columns={this.state.importResultsColumns} noDataIndication="" />
                        </Col>
                      </FormGroup>
                    </CardFooter>
                  </Card>
                </Form>
              )}
        />
      </div>
    );
  }
}

export default ClientsImport;
