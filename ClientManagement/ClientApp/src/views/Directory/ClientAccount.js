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
import 'react-datepicker/dist/react-datepicker.css';
import { isNullOrUndefined, error } from 'util';
import { Link } from 'react-router-dom';
import 'react-bootstrap-table-next/dist/react-bootstrap-table2.min.css';
import 'react-bootstrap-table2-paginator/dist/react-bootstrap-table2-paginator.min.css';


const validationSchema = function (values) {
  return Yup.object().shape({
    name: Yup.string()
      .required('Name is required'),
    firmClientAccountId: Yup.string()
      .max(20, "Your firm's client account id must be 20 characters or less"),
    commonName: Yup.string()
      .max(20, "Common name must be 20 characters or less")
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
  fd.append("commonname", values.commonName);
  fd.append("firmclientaccountid", values.firmClientAccountId);
  fd.append("id", values.id);

  axios.put('/api/clientaccounts/update', fd, {
    headers: { ...authHeader() }
  })
    .then(function (response) {
      setTimeout(() => {
        // At this point save was successful
        values.updateNoticeVisible = true;
        values.updateNoticeMessage = 'Account updated';
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
        values.updateNoticeMessage = 'Account not updated';
        values.updateNoticeStyle = 'danger';
        setSubmitting(false);
      }
    });
};

class ClientAccount extends Component {
  constructor(props) {
    super(props);
    this.state = {
      id: 0,
      firmClientAccountId: '',
      name: '',
      commonName: '',
      deleteClientAccountModal: false,

      // Client Account Activity
      accountActivityPage: 1,
      accountActivityTotalSize: 0,
      accountActivityPerPage: 10,
      accountActivity: [],
      accountActivityColumns: [
        {
          dataField: 'id',
          hidden: true,
          headerStyle: (colum, colIndex) => {
            return { width: '60px' };
          }
        },
        {
          dataField: 'activityDate',
          text: 'Date',
          sort: false,
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'activityTypeName',
          text: 'Type',
          hidden: false,
          headerStyle: (colum, colIndex) => {
            return { width: '120px' };
          }
        },
        {
          dataField: 'activityAmount',
          text: 'Amount',
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'activityDescriptionOverride',
          text: 'Description Override'
        }
      ],
      deleteClientAccountActivityModel: false,
      accountActivityToBeDeleted: [],

      // Client Account Periodic Data
      accountPeriodicDataPage: 1,
      accountPeriodicDataTotalSize: 0,
      accountPeriodicDataPerPage: 10,
      accountPeriodicData: [],
      accountPeriodicDataColumns: [
        {
          dataField: 'id',
          hidden: true
        },
        {
          dataField: 'periodicDataAsOf',
          text: 'Date',
          sort: false,
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        },
        {
          dataField: 'endingBalance',
          text: 'Ending Balance',
          headerStyle: (colum, colIndex) => {
            return { width: '100px' };
          }
        }
      ],
      deleteClientAccountPeriodicDataModel: false,
      accountPeriodicDataToBeDeleted: []
    };

    this.touchAll = this.touchAll.bind(this);

    // Account
    this.toggleClientAccountDeleteModal = this.toggleClientAccountDeleteModal.bind(this);
    this.onClientAccountDelete = this.onClientAccountDelete.bind(this);

    // Account activity
    this.handleAccountActivityTableChange = this.handleAccountActivityTableChange.bind(this);
    this.handleOnSelectActivity = this.handleOnSelectActivity.bind(this);
    this.handleOnSelectAllActivity = this.handleOnSelectAllActivity.bind(this);
    this.toggleClientAccountActivityDeleteModal = this.toggleClientAccountActivityDeleteModal.bind(this);
    this.onClientAccountActivityDelete = this.onClientAccountActivityDelete.bind(this);

    // Account periodic data
    this.handleAccountPeriodicDataTableChange = this.handleAccountPeriodicDataTableChange.bind(this);
    this.handleOnSelectPeriodicData = this.handleOnSelectPeriodicData.bind(this);
    this.handleOnSelectAllPeriodicData = this.handleOnSelectAllPeriodicData.bind(this);
    this.toggleClientAccountPeriodicDataDeleteModal = this.toggleClientAccountPeriodicDataDeleteModal.bind(this);
    this.onClientAccountPeriodicDataDelete = this.onClientAccountPeriodicDataDelete.bind(this);
  }

  componentDidMount() {
    const { match: { params } } = this.props;

    if (params.id !== '0') {
      axios.get(`/api/clientaccounts/clientaccount/${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
        .then(response => {
          if (response.status === 200) {
            this.setState({ id: response.data.id });
            this.setState({ firmClientAccountId: response.data.firmClientAccountId });
            this.setState({ name: response.data.name });
            this.setState({ commonName: response.data.commonName });

            document.getElementById("deleteLink").style.visibility = "visible";

            // Get account activities
            axios.get(`/api/clientaccountactivities?id=${params.id}&size=${this.state.accountActivityPerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ accountActivity: response.data });

                  // We need total size as well
                  axios.get(`/api/clientaccountactivities/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
                    .then(response => {
                      if (response.status === 200) {
                        this.setState({ accountActivityTotalSize: response.data });
                      }
                    });
                }
              })
              .catch(error => {
                window.location.replace('/');
              });

            // Get account periodic data
            axios.get(`/api/clientaccountperiodicdata?id=${params.id}&size=${this.state.accountPeriodicDataPerPage}&page=1`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
              .then(response => {
                if (response.status === 200) {
                  this.setState({ accountPeriodicData: response.data });

                  // We need total size as well
                  axios.get(`/api/clientaccountperiodicdata/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
                    .then(response => {
                      if (response.status === 200) {
                        this.setState({ accountPeriodicDataTotalSize: response.data });
                      }
                    });
                }
              })
              .catch(error => {
                window.location.replace('/');
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

  onClientAccountDelete() {
    axios.delete(`/api/clientaccounts/${this.state.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        this.props.history.push('/directory/clientgroups');
      })
      .catch(function (error) {
        console.log(error);
        if (error.response.status === 409) {
          sessionStorage.removeItem('token');
          window.location.replace('/login');
        }
        else
          alert('There was a problem deleting this account ' + error.response);
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

  touchAll(setTouched, errors) {
    setTouched({
      id: true,
      name: true,
      firmGroupId: true
    }
    );
    this.validateForm(errors);
  }

  toggleClientAccountDeleteModal() {
    this.setState({
      deleteClientAccountModal: !this.state.deleteClientAccountModal
    });
  }

  // Account activity routines
  getClientAccountActivities = (page, sizePerPage) => {
    const { match: { params } } = this.props;

    // Get account activities
    axios.get(`/api/clientaccountactivities?id=${params.id}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ accountActivity: response.data });

          // We need total size as well
          axios.get(`/api/clientaccountactivities/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
            .then(response => {
              if (response.status === 200) {
                this.setState({ accountActivityTotalSize: response.data });
              }
            });
        }
      })
      .catch(error => {
        window.location.replace('/');
      });
  }

  toggleClientAccountActivityDeleteModal() {
    if (this.state.accountActivityToBeDeleted.length > 0)
    {
      this.setState({
        deleteClientAccountActivityModal: !this.state.deleteClientAccountActivityModal
      });
    }
  }

  onClientAccountActivityDelete() {
    if (this.state.accountActivityToBeDeleted.length > 0) {
      let toBeDeletedAccountActivity = JSON.stringify(this.state.accountActivityToBeDeleted);

      const fd = new FormData();
      fd.append("ids", toBeDeletedAccountActivity);


      axios.post('/api/clientaccountactivities/delete', fd, {
        headers: { ...authHeader() }
      })
        .then(response => {
          this.toggleClientAccountActivityDeleteModal();
          this.getClientAccountActivities(1, 20);
        })
        .catch(function (error) {
          console.log(error);
          alert('There was a problem deleting client account selected activity.');
        });
    }
  }

  handleAccountActivityTableChange = (type, { page, sizePerPage }) => {
    this.setState({ accountActivityPage: page });
    this.setState({ accountActivityPerPage: sizePerPage });
    
    this.getClientAccountActivities(page, sizePerPage);
  }

  handleOnSelectActivity = (row, isSelect) => {
    if (isSelect) {
      this.setState(() => ({
        accountActivityToBeDeleted: [...this.state.accountActivityToBeDeleted, row.id]
      }));
    } else {
      this.setState(() => ({
        accountActivityToBeDeleted: this.state.accountActivityToBeDeleted.filter(x => x !== row.id)
      }));
    }
  }

  handleOnSelectAllActivity = (isSelect, rows) => {
    const ids = rows.map(r => r.id);
    if (isSelect) {
      this.setState(() => ({
        accountActivityToBeDeleted: ids
      }));
    } else {
      this.setState(() => ({
        accountActivityToBeDeleted: []
      }));
    }
  }

  // Client Account periodic data routines
  getClientAccountPeriodicData = (page, sizePerPage) => {
    const { match: { params } } = this.props;

    // Get account activities
    axios.get(`/api/clientaccountperiodicdata?id=${params.id}&size=${sizePerPage}&page=${page}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
      .then(response => {
        if (response.status === 200) {
          this.setState({ accountPeriodicData: response.data });

          // We need total size as well
          axios.get(`/api/clientaccountperiodicdata/count?id=${params.id}`, { headers: { ...authHeader(), 'Content-Type': 'application/json' } })
            .then(response => {
              if (response.status === 200) {
                this.setState({ accountPeriodicDataTotalSize: response.data });
              }
            });
        }
      })
      .catch(error => {
        window.location.replace('/');
      });
  }
  
  toggleClientAccountPeriodicDataDeleteModal() {
    if (this.state.accountPeriodicDataToBeDeleted.length > 0) {
      this.setState({
        deleteClientAccountPeriodicDataModel: !this.state.deleteClientAccountPeriodicDataModel
      });
    }
  }

  onClientAccountPeriodicDataDelete() {
    if (this.state.accountPeriodicDataToBeDeleted.length > 0) {
      let toBeDeletedAccountPeriodicData = JSON.stringify(this.state.accountPeriodicDataToBeDeleted);

      const fd = new FormData();
      fd.append("ids", toBeDeletedAccountPeriodicData);


      axios.post('/api/clientaccountperiodicdata/delete', fd, {
        headers: { ...authHeader() }
      })
        .then(response => {
          this.toggleClientAccountPeriodicDataDeleteModal();
          this.getClientAccountPeriodicData(1, 20);
        })
        .catch(function (error) {
          console.log(error);
          alert('There was a problem deleting client account selected periodic data.');
        });
    }
  }

  handleAccountPeriodicDataTableChange = (type, { page, sizePerPage }) => {
    this.setState({ accountPeriodicDataPage: page });
    this.setState({ accountPeriodicDataPerPage: sizePerPage });

    this.getClientAccountPeriodicData(page, sizePerPage);
  }

  handleOnSelectPeriodicData = (row, isSelect) => {
    if (isSelect) {
      this.setState(() => ({
        accountPeriodicDataToBeDeleted: [...this.state.accountPeriodicDataToBeDeleted, row.id]
      }));
    } else {
      this.setState(() => ({
        accountPeriodicDataToBeDeleted: this.state.accountPeriodicDataToBeDeleted.filter(x => x !== row.id)
      }));
    }
  }

  handleOnSelectAllPeriodicData = (isSelect, rows) => {
    const ids = rows.map(r => r.id);
    if (isSelect) {
      this.setState(() => ({
        accountPeriodicDataToBeDeleted: ids
      }));
    } else {
      this.setState(() => ({
        accountPeriodicDataToBeDeleted: []
      }));
    }
  }





  render() {
    const { accountActivityTotalSize, accountActivityPerPage, accountActivityPage } = this.state;
    const accountActivitySelectRow = {
      mode: 'checkbox',
      clickToSelect: true,
      selected: this.state.accountActivityToBeDeleted,
      onSelectAll: this.handleOnSelectAllActivity,
      onSelect: this.handleOnSelectActivity
    };
    const { accountPeriodicDataTotalSize, accountPeriodicDataPerPage, accountPeriodicDataPage } = this.state;
    const accountPeriodicDataSelectRow = {
      mode: 'checkbox',
      clickToSelect: true,
      selected: this.state.accountPeriodicDataToBeDeleted,
      onSelectAll: this.handleOnSelectAllPeriodicData,
      onSelect: this.handleOnSelectPeriodicData
    };

    return (
      <div className="animated fadein">
        <Formik
            enableReinitialize
            initialValues={{
              id: this.state.id, name: this.state.name, commonName: this.state.commonName, firmClientAccountId: this.state.firmClientAccountId, updateNoticeVisible: false, updateNoticeMessage: '', updateNoticeStyle: 'success'
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
                    <CardHeader><i className="fa fa-user" /><strong>Client Account</strong>
                      <div className="card-header-actions">
                        <a id="deleteLink" href="#" onClick={this.toggleClientAccountDeleteModal}><small className="text-muted">Delete</small></a>
                      </div>
                      <Modal isOpen={this.state.deleteClientAccountModal} toggle={this.toggleClientAccountDeleteModal} className={'modal-sm ' + this.props.className}>
                        <ModalHeader toggle={this.toggleClientAccountDeleteModal}>Delete Client Account</ModalHeader>
                        <ModalBody>Are you sure you wish to delete this client account? NOTE: All client activity/history FOR THIS ACCOUNT will be deleted as well.</ModalBody>
                        <ModalFooter>
                          <Button color="primary" onClick={this.onClientAccountDelete}>Delete</Button>{' '}
                          <Button color="secondary" onClick={this.toggleClientAccountDeleteModal}>Cancel</Button>
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
                              placeholder="Account name"
                              autoComplete="name"
                              valid={!errors.name}
                              invalid={touched.name && !!errors.name}
                              autoFocus={true}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.name}
                          />
                          <FormFeedback>{errors.name}</FormFeedback>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="name">Common Name</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="commonName"
                              id="commonName"
                              placeholder="Common account name"
                              autoComplete="commonName"
                              valid={!errors.commonName}
                              invalid={touched.commonName && !!errors.commonName}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}  
                              onBlur={handleBlur}
                              value={values.commonName}
                          />
                          <FormFeedback>{errors.commonName}</FormFeedback>
                          <FormText>A simplified name for the account (optional).</FormText>
                        </Col>
                      </FormGroup>
                      <FormGroup row>
                        <Col md="2">
                          <Label htmlFor="firmClientAccountId">Firm Client Account Id</Label>
                        </Col>
                        <Col xs="12" md="10">
                          <Input type="text"
                              name="firmClientAccountId"
                              id="firmClientAccountId"
                              placeholder="Firm Client Account Id"
                              autoComplete="firmClientAccountId"
                              valid={!errors.firmClientAccountId}
                              invalid={touched.firmClientAccountId && !!errors.firmClientAccountId}
                              required
                              onChange={e => { handleChange(e); values.updateNoticeVisible = false; }}
                              onBlur={handleBlur}
                              value={values.firmClientAccountId}
                          />
                          <FormFeedback>{errors.firmGroupId}</FormFeedback>
                          <FormText>The Firm Client Account Id is the id assigned by the firm.</FormText>
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
            <i className="icon-menu" />Activities in this account{' '}
            <div className="card-header-actions">
              <a id="deleteAccountActivityLink" href="#" onClick={this.toggleClientAccountActivityDeleteModal}><small className="text-muted">Delete Selected Activity</small></a>
            </div>
            <Modal isOpen={this.state.deleteClientAccountActivityModal} toggle={this.toggleClientAccountActivityDeleteModal} className={'modal-sm ' + this.props.className}>
              <ModalHeader toggle={this.toggleClientAccountActivityDeleteModal}>Delete Client Account Activity</ModalHeader>
              <ModalBody>Are you sure you wish to delete client account's selected activity?</ModalBody>
              <ModalFooter>
                <Button color="primary" onClick={this.onClientAccountActivityDelete}>Delete</Button>{' '}
                <Button color="secondary" onClick={this.toggleClientAccountActivityDeleteModal}>Cancel</Button>
              </ModalFooter>
            </Modal>
          </CardHeader>
          <CardBody>
            <BootstrapTable remote striped hover keyField='id' data={this.state.accountActivity} columns={this.state.accountActivityColumns} selectRow={accountActivitySelectRow}  noDataIndication="No activity for this account has been found" pagination={paginationFactory({ page: accountActivityPage , sizePerPage: accountActivityPerPage, totalSize: accountActivityTotalSize })} onTableChange={this.handleAccountActivityTableChange} />
          </CardBody>
        </Card>
        <Card>
          <CardHeader>
            <i className="icon-menu" />Periodic data in this account{' '}
            <div className="card-header-actions">
              <a id="deleteAccountPeriodicDataLink" href="#" onClick={this.toggleClientAccountPeriodicDataDeleteModal}><small className="text-muted">Delete Selected Periodic Data</small></a>
            </div>
            <Modal isOpen={this.state.deleteClientAccountPeriodicDataModel} toggle={this.toggleClientAccountPeriodicDataDeleteModal} className={'modal-sm ' + this.props.className}>
              <ModalHeader toggle={this.toggleClientAccountPeriodicDataDeleteModal}>Delete Client Account Periodic Data</ModalHeader>
              <ModalBody>Are you sure you wish to delete client account's selected periodic data?</ModalBody>
              <ModalFooter>
                <Button color="primary" onClick={this.onClientAccountPeriodicDataDelete}>Delete</Button>{' '}
                <Button color="secondary" onClick={this.toggleClientAccountPeriodicDataDeleteModal}>Cancel</Button>
              </ModalFooter>
            </Modal>
          </CardHeader>
          <CardBody>
            <BootstrapTable remote striped hover keyField='id' data={this.state.accountPeriodicData} columns={this.state.accountPeriodicDataColumns} selectRow={accountPeriodicDataSelectRow} noDataIndication="No periodic data for this account has been found" pagination={paginationFactory({ page: accountPeriodicDataPage, sizePerPage: accountPeriodicDataPerPage, totalSize: accountPeriodicDataTotalSize })} onTableChange={this.handleAccountPeriodicDataTableChange} />
          </CardBody>
        </Card>
      </div>
    );
  }



}

export default withRouter(ClientAccount);

