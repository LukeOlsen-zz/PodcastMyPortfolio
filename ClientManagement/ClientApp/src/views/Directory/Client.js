import React, { Component } from 'react';
import { Alert, Button, Card, CardHeader, CardFooter, CardBody, Col, Form, FormFeedback, FormText, FormGroup, Label, Input, Modal, ModalBody, ModalFooter, ModalHeader, InputGroup, InputGroupAddon } from 'reactstrap';
import BootstrapTable from 'react-bootstrap-table-next';
import paginationFactory from 'react-bootstrap-table2-paginator';

import axios from 'axios';
import { authHeader } from '../../_authHeader';
import { Formik } from 'formik';
import * as Yup from 'yup';
import '../../ValidationForms.css';
import { withRouter } from 'react-router-dom';
import { isNullOrUndefined, error } from 'util';
import { Link } from 'react-router-dom';
import { ClientSendLoginEmail } from '../Directory/ClientSendLoginEmail';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import 'react-bootstrap-table2-paginator/dist/react-bootstrap-table2-paginator.min.css';

const validationSchema = function (values) {
  return Yup.object().shape({
    name: Yup.string()
      .required('Name is required'),
    firmClientId: Yup.string()
      .required('A firm client id is required')
      .max(20, "Your firm's client id must be 20 characters or less"),
    emailAddress: Yup.string()
      .required('An email address is required')
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
  const fd = new FormData();
  fd.append("name", values.name);
  fd.append("firmclientid", values.firmClientId);
  fd.append("emailaddress", values.emailAddress);
  fd.append("username", values.userName);
  fd.append("userpassword", values.userPassword);

  if (values.id === 0) {
    fd.append("clientgroupid", values.clientGroupId);
    axios.post('/api/clients/create', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        setTimeout(() => {
          // At this point save was successful
          values.id = response.data.id;
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Client created';
          values.updateNoticeStyle = 'success';
          values.dataPristine = true;
          setSubmitting(false);
        }, 1000);
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else {
          // Display error for user
          if (!isNullOrUndefined(error.response.data.field)) {
            setFieldError(error.response.data.field, error.response.data.message);
          }
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Client not created';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });
  }
  else {
    fd.append("id", values.id);
    axios.put('/api/clients/update', fd, {
      headers: { ...authHeader() }
    })
      .then(function (response) {
        setTimeout(() => {
          // At this point save was successful
          values.updateNoticeVisible = true;
          values.updateNoticeMessage = 'Client updated';
          values.updateNoticeStyle = 'success';
          values.dataPristine = true;
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
          values.updateNoticeMessage = 'Client not updated';
          values.updateNoticeStyle = 'danger';
          setSubmitting(false);
        }
      });
  }
};


class Client extends Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      clientGroupId: '',
      firmClientId: '',
      name: '',
      emailAddress: '',
      userName: '',
      userPassword: '',
      deleteModal: false,
      accounts: [],
      columns: [
        {
          dataField: 'id',
          hidden: true
        },
        {
          dataField: 'firmClientAccountId',
          text: 'Id',
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'name',
          text: 'Name',
          sort: true,
          formatter: this.renderAccountEditLink
        },
        {
          dataField: 'commonName',
          sort: true,
          text: 'Nickname'
        },
        {
          dataField: 'clientId',
          hidden: true
        }
      ],
      messagePage: 1,
      messageTotalSize: 0,
      messagePerPage: 10,
      messages: [],
      messageColumns: [
        {
          dataField: 'id',
          hidden: true
        },
        {
          dataField: 'clientId',
          hidden: true
        },
        {
          dataField: 'messageTypeName',
          text: 'Type',
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'clientMessage',
          text: 'Message',
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'expiresOn',
          text: 'Expires On',
          sort: false,
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'receivedByClient',
          text: 'Received',
          sort: false,
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        }
      ],
      deleteClientMessageModel: false,
      clientMessagesToBeDeleted: []
    };

    this.touchAll = this.touchAll.bind(this);
    this.toggleDeleteModal = this.toggleDeleteModal.bind(this);
    this.onDelete = this.onDelete.bind(this);

    // Client messages
    this.handleClientMessageTableChange = this.handleClientMessageTableChange.bind(this);
    this.handleOnSelectClientMessage = this.handleOnSelectClientMessage.bind(this);
    this.handleOnSelectAllClientMessages = this.handleOnSelectAllClientMessages.bind(this);
    this.toggleClientMessageDeleteModal = this.toggleClientMessageDeleteModal.bind(this);
    this.onClientMessageDelete = this.onClientMessageDelete.bind(this);
  }

  componentDidMount() {
    const { match: { params } } = this.props;

    if (params.id !== '0') {
      axios.get(`/api/clients/client/${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
        .then(response => {
          if (response.status === 200) {
            this.setState({ id: response.data.id });
            this.setState({ clientGroupId: response.data.clientGroupId });
            this.setState({ firmClientId: response.data.firmClientId });
            this.setState({ name: response.data.name });
            this.setState({ emailAddress: response.data.emailAddress });
            this.setState({ userName: response.data.userName });
            document.getElementById("deleteLink").style.visibility = "visible";

            // Get accounts for client
            axios.get(`/api/clientaccounts/${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ accounts: response.data });
                }
              });

            // Get messages for client
            axios.get(`/api/clientmessages?id=${params.id}&size=${this.state.messagePerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ messages: response.data });

                  // We need total size as well
                  axios.get(`/api/clientmessages/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
                    .then(response => {
                      if (response.status === 200) {
                        this.setState({ messageTotalSize: response.data });
                      }
                    });
                }
              });

          }
        })
        .catch(error => {
          window.location.replace('/');
        });
    }
    else {
      document.getElementById("deleteLink").style.visibility = "hidden";
    }
  }


  renderAccountEditLink(cell, row, rowIndex) {
    return (
      <span>
        <Link to={{ pathname: `/directory/clients/clientaccount/${row.id}`}}>{cell}</Link>
      </span>
    );
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

  touchAll(setTouched, errors) {
    setTouched({
      id: true,
      name: true,
      firmClientId: true,
      emailAddress: true,
      userName: true,
      userPassword: true
    }
    );
    this.validateForm(errors);
  }

  toggleDeleteModal() {
    this.setState({
      deleteModal: !this.state.deleteModal
    });
  }

  onDelete() {
    axios.delete(`/api/clients/${this.state.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        this.props.history.push('/directory/clients');
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else
          alert('There was a problem deleting this client ' + error.response);
      });
  }


  // Client message data routines
  getClientMessages = (page, sizePerPage) => {
    const { match: { params } } = this.props;

    // Get messages for client
    axios.get(`/api/clientmessages?id=${params.id}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ messages: response.data });

          // We need total size as well
          axios.get(`/api/clientmessages/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
            .then(response => {
              if (response.status === 200) {
                this.setState({ messageTotalSize: response.data });
              }
            });
        }
      })
      .catch(error => {
        window.location.replace('/');
      });
  }

  toggleClientMessageDeleteModal() {
    if (this.state.clientMessagesToBeDeleted.length > 0) {
      this.setState({
        deleteClientMessageModel: !this.state.deleteClientMessageModel
      });
    }
  }

  onClientMessageDelete() {
    if (this.state.clientMessagesToBeDeleted.length > 0) {
      let toBeDeletedClientMessages = JSON.stringify(this.state.clientMessagesToBeDeleted);

      const fd = new FormData();
      fd.append("ids", toBeDeletedClientMessages);


      axios.post('/api/clientmessages/delete', fd, {
        headers: { ...authHeader() }
      })
        .then(response => {
          this.toggleClientMessageDeleteModal();
          this.getClientMessages(1, 20);
        })
        .catch(function (error) {
          console.log(error);
          alert('There was a problem deleting client messages.');
        });
    }
  }

  handleClientMessageTableChange = (type, { page, sizePerPage }) => {
    this.setState({ messagePage: page });
    this.setState({ messagePerPage: sizePerPage });

    this.getClientMessages(page, sizePerPage);
  }

  handleOnSelectClientMessage = (row, isSelect) => {
    if (isSelect) {
      this.setState(() => ({
        clientMessagesToBeDeleted: [...this.state.clientMessagesToBeDeleted, row.id]
      }));
    } else {
      this.setState(() => ({
        clientMessagesToBeDeleted: this.state.clientMessagesToBeDeleted.filter(x => x !== row.id)
      }));
    }
  }

  handleOnSelectAllClientMessages = (isSelect, rows) => {
    const ids = rows.map(r => r.id);
    if (isSelect) {
      this.setState(() => ({
        clientMessagesToBeDeleted: ids
      }));
    } else {
      this.setState(() => ({
        clientMessagesToBeDeleted: []
      }));
    }
  }


  render() {
    const { messageTotalSize, messagePerPage, messagePage } = this.state;
    const clientMessageSelectRow = {
      mode: 'checkbox',
      clickToSelect: true,
      selected: this.state.clientMessagesToBeDeleted,
      onSelectAll: this.handleOnSelectAllClientMessages,
      onSelect: this.handleOnSelectClientMessage
    };

    return (
      <div className="animated fadein">
        <Formik
            enableReinitialize
            initialValues={{
              id: this.state.id, name: this.state.name, clientGroupId: this.props.match.params.clientgroupid, firmClientId: this.state.firmClientId, emailAddress: this.state.emailAddress, 
              userName: this.state.userName, userPassword: this.state.userPassword, 
              updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success', dataPristine: true
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
                    <CardHeader><i className="fa fa-user" /><strong>Client</strong>
                      <div className="card-header-actions">
                        <a id="deleteLink" href="#" onClick={this.toggleDeleteModal}><small className="text-muted">Delete</small></a>
                      </div>
                      <Modal isOpen={this.state.deleteModal} toggle={this.toggleDeleteModal} className={'modal-sm ' + this.props.className}>
                        <ModalHeader toggle={this.toggleDeleteModal}>Delete Client</ModalHeader>
                        <ModalBody>Are you sure you wish to delete this client?</ModalBody>
                        <ModalFooter>
                          <Button color="primary" onClick={this.onDelete}>Delete</Button>{' '}
                          <Button color="secondary" onClick={this.toggleDeleteModal}>Cancel</Button>
                        </ModalFooter>
                      </Modal>
                    </CardHeader>
                    <CardBody>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="name"
                              id="name"
                              placeholder="Client name"
                              autoComplete="name"
                              valid={!errors.name}
                              invalid={touched.name && !!errors.name}
                              autoFocus="true"
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; values.dataPristine = false;}}
                              onBlur={handleBlur}
                              value={values.name}
                          />
                          <FormFeedback>{errors.name}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="firmClientId">Firm Client Id</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="firmClientId"
                              id="firmClientId"
                              placeholder="Firm Client Id"
                              autoComplete="firmClientId"
                              valid={!errors.firmClientId}
                              invalid={touched.firmClientId && !!errors.firmClientId}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; values.dataPristine = false; }}
                              onBlur={handleBlur}
                              value={values.firmClientId}
                          />
                          <FormFeedback>{errors.firmClientId}</FormFeedback>
                          <FormText>The Firm Client Id is the id assigned by the firm.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="firmGroupId">Email Address</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="email"
                              name="emailAddress"
                              id="emailAddress"
                              placeholder="Email Address"
                              autoComplete="emailAddress"
                              valid={!errors.emailAddress}
                              invalid={touched.emailAddress && !!errors.emailAddress}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; values.dataPristine = false; }}
                              onBlur={handleBlur}
                              value={values.emailAddress}
                          />
                          <FormFeedback>{errors.emailAddress}</FormFeedback>
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
                              autoComplete="userName"
                              valid={!errors.userName}
                              invalid={touched.userName && !!errors.userName}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; values.dataPristine = false; this.clientsendemail.reset();}}
                              onBlur={handleBlur}
                              value={values.userName}
                          />
                          <FormFeedback>{errors.userName}</FormFeedback>
                          <FormText>If blank a user won't be able to use resources</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="userPassword">User Password</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="password"
                              name="userPassword"
                              id="userPassword"
                              placeholder="**********************"
                              autoComplete="userPassword"
                              valid={!errors.userPassword}
                              invalid={touched.userPassword && !!errors.userPassword}
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; values.dataPristine = false; this.clientsendemail.reset(); }}
                              onBlur={handleBlur}
                              value={values.userPassword}
                          />
                          <FormFeedback>{errors.userPassword}</FormFeedback>
                          <FormText>You can only set a new password. Leave blank if you do not wish to change.</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2"/>
                        <Col md="10">
                          <ClientSendLoginEmail disabled={!values.dataPristine} clientid={this.state.id} onRef={ref => this.clientsendemail = ref} />
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
        <Card>
          <CardHeader>
            <i className="icon-menu" />Accounts
          </CardHeader>
          <CardBody>
            <BootstrapTable remote striped hover keyField='id' data={this.state.accounts} columns={this.state.columns} noDataIndication="No accounts for this client have been found" />
          </CardBody>
        </Card>
        <Card>
          <CardHeader>
            <i className="icon-menu" />Messages{' '}
            <div className="card-header-actions">
              <a id="deleteClientMessagLink" href="#" onClick={this.toggleClientMessageDeleteModal}><small className="text-muted">Delete Selected Client Messages</small></a>
            </div>
            <Modal isOpen={this.state.deleteClientMessageModel} toggle={this.toggleClientMessageDeleteModal} className={'modal-sm ' + this.props.className}>
              <ModalHeader toggle={this.toggleClientMessageDeleteModal}>Delete Client Messages</ModalHeader>
              <ModalBody>Are you sure you wish to delete the selected clients' messages?</ModalBody>
              <ModalFooter>
                <Button color="primary" onClick={this.onClientMessageDelete}>Delete</Button>{' '}
                <Button color="secondary" onClick={this.toggleClientMessageDeleteModal}>Cancel</Button>
              </ModalFooter>
            </Modal>
          </CardHeader>
          <CardBody>
            <BootstrapTable remote striped hover keyField='id' data={this.state.messages} columns={this.state.messageColumns} selectRow={clientMessageSelectRow} noDataIndication="No messages for this client has been found" pagination={paginationFactory({ page: messagePage, sizePerPage: messagePerPage, totalSize: messageTotalSize })} onTableChange={this.handleClientMessageTableChange} />
          </CardBody>
        </Card>
      </div>
    );
  }

}



export default withRouter(Client);
