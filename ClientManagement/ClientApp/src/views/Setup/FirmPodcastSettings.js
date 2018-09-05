import React from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input } from 'reactstrap';
import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import { isNullOrUndefined, error } from 'util';

const validationSchema = function (values) {
  return Yup.object().shape({
    welcomemessage: Yup.string()
      .required('A default welcome message is required')
      .nullable(),
    voiceid: Yup.number()
      .required('A default voice is required'),
    podcastcontactemail: Yup.string()
      .email('Not a valid email address')
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
  if (isNullOrUndefined(values.logo) || (values.logo.size <= 102400000 && values.logo.type === 'image/jpeg')) {
    const fd = new FormData();
    if (!isNullOrUndefined(values.logo)) {
      fd.append("file", values.logo, 'podcastlogo.jpg');
    }

    fd.append("id", values.id);
    fd.append("podcastwelcomemessage", values.welcomemessage);
    fd.append("podcastnotfoundmessage", values.notfoundmessage);
    fd.append("podcastvoiceid", values.voiceid);
    fd.append("podcastfirmsiteurl", values.siteurl);
    fd.append("podcastfirmlogoid", values.logoid);
    fd.append("podcastcontactname", values.podcastcontactname);
    fd.append("podcastcontactemail", values.podcastcontactemail);
    fd.append("podcastdescription", values.podcastdescription);


    axios.put('api/firmpodcastsettings/update', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        // Get new logo src url
        if (!isNullOrUndefined(response.data.podcastFirmLogoURL)) {
          document.getElementById('logoimage').setAttribute('src', response.data.podcastFirmLogoURL);
        }

        setTimeout(() => {
          // At this point save was successful
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Podcast settings updated';
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
          values.updateNoticeMessage = 'Podcast settings NOT updated';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });

  }
  else {
    setFieldError('logo', 'Image must be a jpeg less than 10MB.');
    setSubmitting(false);
  }
};

class FirmPodcastSettings extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      welcomemessage: '',
      notfoundmessage: '',
      voiceid: 0,
      siteurl: '',
      logourl: '',
      logoid: '',
      voices: [],
      podcastcontactname: '',
      podcastcontactemail: '',
      podcastdescription: ''
    };

    this.touchAll = this.touchAll.bind(this);
  }

  componentDidMount() {
    axios.get('/api/firmpodcastsettings', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ id: response.data.id });
          this.setState({ welcomemessage: response.data.podcastWelcomeMessage });
          this.setState({ notfoundmessage: response.data.podcastNotFoundMessage });
          this.setState({ voiceid: response.data.podcastVoiceId });
          this.setState({ siteurl: response.data.podcastFirmSiteURL });
          this.setState({ logourl: response.data.podcastFirmLogoURL });
          this.setState({ logoid: response.data.podcastFirmLogoId });
          this.setState({ podcastcontactname: response.data.podcastContactName });
          this.setState({ podcastcontactemail: response.data.podcastContactEmail });
          this.setState({ podcastdescription: response.data.podcastDescription });
        }
      })
      .catch(error => {
        window.location.replace('/');
      });

      // Get all voice choices for drop-down
    axios.get('/api/voices/all', { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ voices: response.data });
        }
      })
      .catch(error => {
        window.location.replace('/');
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
      id: true,
      welcomemessage: true,
      notfoundmessage: true,
      voiceid: true,
      logo: true,
      logourl: true,
      logoid: true,
      siteurl: true,
      podcastcontactname: true,
      podcastcontactemail: true,
      podcastdescription: true
    }
    );
    this.validateForm(errors);
  }


  render() {
    return (
      <div className="animated fadeIn">
        <Formik
            enableReinitialize
            initialValues={{
              id: this.state.id, welcomemessage: this.state.welcomemessage, notfoundmessage: this.state.notfoundmessage,  voiceid: this.state.voiceid, logo: null, logourl: this.state.logourl, logoid: this.state.logoid,
              siteurl: this.state.siteurl,
              podcastcontactname: this.state.podcastcontactname, podcastcontactemail: this.state.podcastcontactemail, podcastdescription: this.state.podcastdescription,
              updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success'
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
                    <CardHeader><i className="fa fa-user" /><strong>Podcast Settings</strong></CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Welcome Message</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="welcomemessage"
                              id="welcomemessage"
                              placeholder="Welcome Message"
                              autoComplete="welcomemessage"
                              valid={!errors.welcomemessage}
                              invalid={touched.welcomemessage && !!errors.welcomemessage}
                              autoFocus={true}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.welcomemessage}
                          />
                          <FormFeedback>{errors.welcomemessage}</FormFeedback>
                          <FormText>The welcome message will be delivered at the beginning of every podcast. While not limited on size it is best to keep this short.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Description</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="podcastdescription"
                              id="podcastdescription"
                              placeholder="Podcast Description"
                              autoComplete="podcastdescription"
                              valid={!errors.podcastdescription}
                              invalid={touched.podcastdescription && !!errors.podcastdescription}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.podcastdescription}
                          />
                          <FormFeedback>{errors.podcastdescription}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Not Found Message</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="notfoundmessage"
                              id="notfoundmessage"
                              placeholder="Not Found Message"
                              autoComplete="notfoundmessage"
                              valid={!errors.notfoundmessage}
                              invalid={touched.notfoundmessage && !!errors.notfoundmessage}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.notfoundmessage}
                          />
                          <FormFeedback>{errors.welcomemessage}</FormFeedback>
                          <FormText>The welcome message will be delivered at the beginning of every podcast. While not limited on size it is best to keep this short.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Site Link</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="siteurl"
                              id="siteurl"
                              placeholder="Podcast Site Link (URL)"
                              autoComplete="siteurl"
                              valid={!errors.siteurl}
                              invalid={touched.siteurl && !!errors.siteurl}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.siteurl}
                          />
                          <FormFeedback>{errors.siteurl}</FormFeedback>
                          <FormText>This URL will be embedded into the Podcast feed.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Podcast Contact Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="podcastcontactname"
                              id="podcastcontactname"
                              placeholder="Podcast Contact Name"
                              autoComplete="podcastcontactname"
                              valid={!errors.podcastcontactname}
                              invalid={touched.podcastcontactname && !!errors.podcastcontactname}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.podcastcontactname}
                          />
                          <FormFeedback>{errors.podcastcontactname}</FormFeedback>
                          <FormText>The name that will be used in correspondance to the client</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Podcast Contact Email</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="email"
                              name="podcastcontactemail"
                              id="podcastcontactemail"
                              placeholder="Podcast Contact Email"
                              autoComplete="podcastcontactemail"
                              valid={!errors.podcastcontactemail}
                              invalid={touched.podcastcontactemail && !!errors.podcastcontactemail}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.podcastcontactemail}
                          />
                          <FormFeedback>{errors.podcastcontactemail}</FormFeedback>
                          <FormText>The FROM email that will be used in correspondance to the client</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Default Voice</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="select"
                              name="voiceid"
                              id="voiceid"
                              valid={!errors.voiceid}
                              invalid={touched.voiceid && !!errors.voiceid}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.voiceid}
                          >
                          {
                              this.state.voices && this.state.voices.length > 0 && this.state.voices.map((voice) => {
                              return <option key={voice.id} value={voice.id}> {voice.name}</option>; })
                          }
                          </Input>
                          <FormFeedback>{errors.voiceid}</FormFeedback>
                          <FormText>This will only impact new podcasts.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="logo">Logo</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="file"
                              name="logo"
                              id="logo"
                              placeholder="Podcast logo"
                              autoComplete="logo"
                              valid={!errors.logo}
                              invalid={touched.logo && !!errors.logo}
                              onChange={(event) => { setFieldValue("logo", event.currentTarget.files[0]); values.updateNoticeVisible = false; }}
                          />
                          <FormFeedback>{errors.logo}</FormFeedback>
                          <FormText>The logo that will be displayed on podcast players (jpeg at 72 dpi using the RGB color space recommended).</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2" />
                        <Col xs="12" md="10">
                          <img id="logoimage" style={{"max-width": "100%", "height": "auto"}} src={this.state.logourl}/>
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

export default FirmPodcastSettings;
