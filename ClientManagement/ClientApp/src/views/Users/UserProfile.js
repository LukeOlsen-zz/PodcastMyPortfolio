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
    fullName: Yup.string()
      .min(5, 'Full name has to be at least 5 characters')
      .max(100, 'Full name cannot be more than 100 characters')
      .required('Full name is required'),
    userName: Yup.string()
      .min(5, 'Username has to be at least 5 characters')
      .max(20,'Username cannot be more than 20 characters')
      .required('Username is required'),
    email: Yup.string()
      .email('Invalid email address')
      .required('Email is required'),
    password: Yup.string()
      .min(5, `Password has to be at least ${5} characters!`),
    confirmPassword: Yup.string()
      .oneOf([values.password], 'Passwords must match')
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
  // Only upload if profileimage is either null or less than 10MB
  if (isNullOrUndefined(values.profileImage) || (values.profileImage.size <= 102400000 && values.profileImage.type === 'image/jpeg'))
  {
    if (values.password === values.confirmPassword) {
      const fd = new FormData();

      if (!isNullOrUndefined(values.profileImage)) {
        fd.append("file", values.profileImage, 'profile.jpg');
      }

      fd.append("username", values.userName);
      fd.append("fullname", values.fullName);
      fd.append("email", values.email);
      if (values.password === "*******************") {
        fd.append("password", null);
      }
      else {
        fd.append("password", values.password);
      }

      axios.post('/api/userprofiles/update', fd, {
        headers: { ...authHeader() }
      })
        .then(function (response) {
          sessionStorage.setItem('userfullname', values.fullName);

          // If we posted a profile image we will need to re-read it and set the UI
          if (!isNullOrUndefined(values.profileImage)) {
            // We may need to re-read the user profile image
            axios.get('/api/userprofiles', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  if (!isNullOrUndefined(response.data.profileImage)) {
                    sessionStorage.setItem('profileimage', response.data.profileImage);
                    document.getElementById('profileImageHeader').setAttribute('src', "data:image/jpeg;base64," + response.data.profileImage);
                  }
                }
              })
              .catch(error => {
                if (error.response.status === 400) {
                  sessionStorage.removeItem('token');
                  window.location.replace('/login');
                }
              });
          }

          setTimeout(() => {
            // At this point save was successful
            values.updateNoticeVisible = true;
            values.updateNoticeMessage = 'Profile updated';
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
            values.updateNoticeMessage = 'Profile NOT updated';
            values.updateNoticeStyle = 'danger';
            setSubmitting(false);
          }
        });
    }
    else {
      setFieldError('confirmPassword', "Password doesn't match");
      setSubmitting(false);
    }
  }
  else {
    setFieldError('profileImage', 'Image must be a jpeg less than 10MB.');
    setSubmitting(false);
  }


};

class UserProfile extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      fullName: '',
      userName: '',
      email: ''
    };
    this.touchAll = this.touchAll.bind(this);
  }

  componentDidMount() {
    axios.get('/api/userprofiles', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ fullName: response.data.fullName });
          this.setState({ userName: response.data.userName });
          this.setState({ email: response.data.email });
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
      fullName: true,
      userName: true,
      email: true,
      profileImage: true,
      password: true,
      confirmPassword: true
    }
    );
    this.validateForm(errors);
  }

  render() {
    return (
      <div className="animated fadeIn">
        <Formik
            enableReinitialize
            initialValues={{ fullName: this.state.fullName, userName: this.state.userName, email: this.state.email, password: '*******************', confirmPassword: '*******************', profileImage: null, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success'}}
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
                    <CardHeader><i className="fa fa-user" /><strong>Profile</strong></CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="fullName">Full Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="fullName"
                              id="fullName"
                              placeholder="Full Name"
                              autoComplete="given-name"
                              valid={!errors.fullName}
                              invalid={touched.fullName && !!errors.fullName}
                              autoFocus={true}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.fullName}
                          />
                          <FormFeedback>{errors.fullName}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="userName">User Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="userName"
                              id="userName"
                              placeholder="User Name"
                              autoComplete="username"
                              valid={!errors.userName}
                              invalid={touched.userName && !!errors.userName}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.userName}
                          />
                          <FormFeedback>{errors.userName}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="email">Email</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="email"
                              name="email"
                              id="email"
                              placeholder="Email"
                              autoComplete="email"
                              valid={!errors.email}
                              invalid={touched.email && !!errors.email}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.email}
                          />
                          <FormFeedback>{errors.email}</FormFeedback>
                        </Col>
                        </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="email">Password</Label>
                        </Col>
                        <Col xs="12" md="5">
                          <Input type="password"
                              name="password"
                              id="password"
                              placeholder="Password (required only if you are changing it)"
                              autoComplete="new-password"
                              valid={!errors.password}
                              invalid={touched.password && !!errors.password}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.password}
                          />
                          <FormFeedback>{errors.password}</FormFeedback>
                        </Col>
                        <Col xs="12" md="5">
                          <Input type="password"
                              name="confirmPassword"
                              id="confirmPassword"
                              placeholder="Confirm password"
                              autoComplete="new-password"
                              valid={!errors.confirmPassword}
                              invalid={touched.confirmPassword && !!errors.confirmPassword}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.confirmPassword}
                            />
                          <FormFeedback>{errors.confirmPassword}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="profileImage">Profile image</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="file"
                              name="profileImage"
                              id="profileImage"
                              placeholder="Profile image"
                              valid={!errors.profileImage}
                              invalid={touched.profileImage && !!errors.profileImage}
                              autoComplete="profileImage"
                              onChange={(event) => { setFieldValue("profileImage", event.currentTarget.files[0]); values.updateNoticeVisible = false; }}
                          />
                          <FormFeedback>{errors.profileImage}</FormFeedback>
                          <FormText>If no profile image is specified then the existing one won't be overwritten.</FormText>
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
            )}
        />
      </div>
    );
  }
}

export default UserProfile;
