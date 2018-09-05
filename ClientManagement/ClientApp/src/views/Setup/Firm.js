import React from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';

import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import { isNullOrUndefined } from 'util';


const validationSchema = function (values) {
  return Yup.object().shape({
    name: Yup.string()
      .min(1, 'Name has to be at least 1 character')
      .max(100, 'Name cannot be more than 100 characters')
      .required('Name is required')
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
  const fd = new FormData();
  fd.append("id", values.id);
  fd.append("name", values.name);

  axios.put('api/firms/update', fd, {
    headers: { ...authHeader() }
  })
    .then(function (response) {
      setTimeout(() => {
        // At this point save was successful
        values.updateNoticeVisible = true;
        values.updateNoticeMessage = 'Firm updated';
        values.updateNoticeStyle = 'success';
        setSubmitting(false);
      }, 1000);
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
        values.updateNoticeMessage = 'Firm NOT updated';
        values.updateNoticeStyle = 'danger';
        setSubmitting(false);
      }
    });
};

class Firm extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      name: ''
    };
    this.touchAll = this.touchAll.bind(this);
  }

  componentDidMount() {
    axios.get('api/firms', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ id: response.data.id });
          this.setState({ name: response.data.name });
        }
      })
      .catch(error => {
        // user profiles are ALWAYS available for the logged in user. If an error occures restart back at login
        sessionStorage.removeItem('token');
        window.location.replace('/login');
      });
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
      name: true
    }
    );
    this.validateForm(errors);
  }

  render() {
    return (
      <div className="animated fadeIn">
        <Formik
            enableReinitialize
            initialValues={{ id: this.state.id, name: this.state.name, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success' }}
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
            }) => 
                <Form onSubmit={handleSubmit} noValidate name='simpleForm'>
                  <Card>
                    <CardHeader><i className="fa fa-user" /><strong>Firm</strong></CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="name"
                              id="name"
                              placeholder="Firm Name"
                              autoComplete="given-name"
                              valid={!errors.name}
                              invalid={touched.name && !!errors.name}
                              autoFocus="true"
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.name}
                          />
                          <FormFeedback>{errors.name}</FormFeedback>
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
                        </Col>
                      </FormGroup>
                    </CardFooter>
                  </Card>
                </Form>
              }
        />
      </div>
    );
  }
}

export default Firm;
